using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ResortEmpire
{
    // Balancing rules WR7 and WR8 of the Build document (§4.8) over the default config.json
    // (SCRUM-172, NFR-10). Like wave_sim.py the simulation is static: Workers are placed before
    // the wave and never moved during it, and no Energy is used. A wave always ends with empty
    // queues, so every wave is simulated on its own with the resort the player has at that point.
    public class WaveBalanceRulesTests
    {
        private const int FiveStars = 5;
        private const int Perfect = 6;
        private const int StartLevel = 1;
        private const int HighestLevel = 3; // FR-24: upgrades end at level 3.
        private const int MaxSteps = 36000; // one hour of game time at 0.1 s per step

        // Build document §4.3: Pool is unlocked after wave 3 and Bungalows after wave 6 (FR-30).
        private static readonly Dictionary<string, int> FirstWaveOfBuilding = new(StringComparer.Ordinal)
        {
            { "Reception", 1 }, { "Restaurant", 1 }, { "BeachBar", 1 }, { "Pool", 4 }, { "Bungalows", 7 }
        };

        private MainConfig config;

        [OneTimeSetUp]
        public void LoadDefaultConfig()
        {
            UtilConfig.loadConfig();
            config = UtilConfig.getConfig();
            Assert.IsNotNull(config, "config.json was not loaded.");
        }

        // WR7: from wave 3 on, except the debut waves 4 and 7, even the best static placement
        // without moves, upgrades and Energy must stay below 5★, otherwise the wave tests nothing.
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(8)]
        public void Wr7BestStaticPlacementWithoutUpgradesStaysBelowFiveStars(int waveId)
        {
            WaveResult best = BestStaticPlacement(waveId, StartLevel);

            Assert.GreaterOrEqual(best.Rating, 1, best.ToString());
            Assert.Less(best.Rating, FiveStars, best.ToString());
        }

        // WR8: a fully prepared resort, every building at the highest level, reaches 5★
        // in every wave without Energy.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void Wr8FullyUpgradedResortReachesFiveStarsInEveryWave(int waveId)
        {
            WaveResult best = BestStaticPlacement(waveId, HighestLevel);

            Assert.GreaterOrEqual(best.Rating, FiveStars, best.ToString());
        }

        // The simulation relies on the wave table asking only for needs of buildings the player
        // can own by then (WR9), so that debut waves 4 and 7 match the unlocks.
        [Test]
        public void WavesAskOnlyForNeedsOfBuildingsAvailableInThem()
        {
            Assert.AreEqual(8, config.wave_settings.waves.Length);

            foreach (global::WavesConfig wave in config.wave_settings.waves)
            {
                var available = AvailableBuildings(wave.wave_id).Values.ToHashSet();
                foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
                {
                    if (!available.Contains(need))
                        Assert.AreEqual(0, Demand(wave.needs, need), $"wave {wave.wave_id}, {need}");
                }
            }
        }

        // Prints every placement of every wave for docs/ and for tuning; run on demand.
        [Test, Explicit("Prints the balance table, run on demand.")]
        public void PrintBalanceTable()
        {
            var table = new StringBuilder();
            table.AppendLine("| Wave | Level | Without Worker | Satisfaction | Rating | Duration |");
            table.AppendLine("|---|---|---|---|---|---|");

            foreach (int level in new[] { StartLevel, HighestLevel })
            {
                foreach (global::WavesConfig wave in config.wave_settings.waves)
                {
                    foreach (string unstaffed in AvailableBuildings(wave.wave_id).Keys)
                    {
                        WaveResult result = RunWave(wave.wave_id, level, unstaffed);
                        table.AppendLine($"| {result.WaveId} | {result.Level} | {result.UnstaffedBuildingId} | " +
                            $"{result.Percent} % | {RatingText(result.Rating)} | {result.Steps / 10f:0.0} s |");
                    }
                }
            }

            TestContext.WriteLine(table.ToString());
            UnityEngine.Debug.Log(table.ToString());
        }

        private WaveResult BestStaticPlacement(int waveId, int level)
        {
            var results = AvailableBuildings(waveId).Keys
                .Select(unstaffed => RunWave(waveId, level, unstaffed))
                .ToList();

            // Exactly one building stays without a Worker (FR-11), so every choice of it is tried.
            Assert.AreEqual(AvailableBuildings(waveId).Count, results.Count);
            return results.OrderByDescending(result => result.Percent).First();
        }

        private WaveResult RunWave(int waveId, int level, string unstaffedBuildingId)
        {
            global::WavesConfig wave = config.wave_settings.waves.Single(item => item.wave_id == waveId);
            var simulation = new GameSimulation(config.needStateConfig.bottleneckThreshold);
            var allBuildings = new Dictionary<string, NeedType>(StringComparer.Ordinal);
            var available = AvailableBuildings(waveId);

            foreach (global::BuildingsConfig building in config.buildings)
            {
                NeedType need = ParseNeed(building.need);
                allBuildings.Add(building.id, need);
                if (available.ContainsKey(building.id))
                    simulation.RegisterBuildingService(need,
                        new BuildingService(building.levels, level, building.service_time));
            }

            var workers = available.Keys
                .Where(id => id != unstaffedBuildingId)
                .Select((id, index) => new global::WorkerInitialState { id = $"Worker{index + 1}", buildingId = id })
                .ToArray();
            simulation.InitializeWorkers(available, workers, config.workerConfig.unstaffedPerformanceMultiplier);

            var cycle = new GameCycle(simulation, new[] { wave }, config.wave_settings.wave_timer,
                allBuildings, new HashSet<string>(available.Keys, StringComparer.Ordinal));
            cycle.StartNextWave();

            int steps = 0;
            while (cycle.CurrentState == GameState.WaveInProgress && steps < MaxSteps)
            {
                cycle.UpdateStep();
                steps++;
            }

            Assert.AreNotEqual(GameState.WaveInProgress, cycle.CurrentState,
                $"Wave {waveId} at level {level} without a Worker at {unstaffedBuildingId} did not end.");

            int percent = WaveSatisfactionCalculator.CalculatePercent(simulation, config.wave_settings);
            return new WaveResult(waveId, level, unstaffedBuildingId, percent, UtilConfig.getWaveRating(percent), steps);
        }

        private Dictionary<string, NeedType> AvailableBuildings(int waveId)
        {
            var available = new Dictionary<string, NeedType>(StringComparer.Ordinal);
            foreach (global::BuildingsConfig building in config.buildings)
            {
                Assert.IsTrue(FirstWaveOfBuilding.TryGetValue(building.id, out int firstWave),
                    $"Unknown building {building.id} in config.json.");
                if (firstWave <= waveId)
                    available.Add(building.id, ParseNeed(building.need));
            }
            return available;
        }

        private static NeedType ParseNeed(string value)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "rooms": return NeedType.Rooms;
                case "food": return NeedType.Food;
                case "drinks": return NeedType.Drinks;
                case "relax": return NeedType.Relax;
                case "ocean_view": return NeedType.OceanView;
                default: throw new ArgumentException($"Unknown need '{value}' in config.json.");
            }
        }

        private static int Demand(global::NeedsConfig needs, NeedType need)
        {
            switch (need)
            {
                case NeedType.Rooms: return needs.rooms;
                case NeedType.Food: return needs.food;
                case NeedType.Drinks: return needs.drinks;
                case NeedType.Relax: return needs.relax;
                case NeedType.OceanView: return needs.ocean_view;
                default: return 0;
            }
        }

        private static string RatingText(int rating) => rating == Perfect ? "Perfect" : $"{rating}★";

        private readonly struct WaveResult
        {
            public WaveResult(int waveId, int level, string unstaffedBuildingId, int percent, int rating, int steps)
            {
                WaveId = waveId;
                Level = level;
                UnstaffedBuildingId = unstaffedBuildingId;
                Percent = percent;
                Rating = rating;
                Steps = steps;
            }

            public int WaveId { get; }
            public int Level { get; }
            public string UnstaffedBuildingId { get; }
            public int Percent { get; }
            public int Rating { get; }
            public int Steps { get; }

            public override string ToString() =>
                $"Wave {WaveId}, level {Level}: the best placement leaves {UnstaffedBuildingId} without a Worker " +
                $"and ends with {Percent} % ({RatingText(Rating)}) after {Steps / 10f:0.0} s.";
        }
    }
}
