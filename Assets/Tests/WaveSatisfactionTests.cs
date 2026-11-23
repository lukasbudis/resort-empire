using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class WaveSatisfactionTests
    {
        private static void Step(BuildingService service, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                service.Step();
        }

        private static IReadOnlyList<UnitWaitingTime> Waits(params double[] waitSeconds)
        {
            var samples = new List<UnitWaitingTime>(waitSeconds.Length);
            foreach (double seconds in waitSeconds)
            {
                if (seconds <= 0d)
                {
                    var instant = new BuildingService(1, 1f);
                    instant.AddArrivals(1);
                    samples.Add(instant.WaitingTimes[0]);
                }
                else
                {
                    int ticks = (int)Math.Round(seconds / 0.1d);
                    var queued = new BuildingService(1, (float)seconds);
                    queued.AddArrivals(2);
                    Step(queued, ticks);
                    samples.Add(queued.WaitingTimes[1]);
                }
            }
            return samples;
        }


        [TestCase(0.0, 100.0)]
        [TestCase(10.0, 100.0)]
        [TestCase(20.0, 62.5)]
        [TestCase(30.0, 25.0)]
        [TestCase(60.0, 25.0)]
        public void UnitSatisfaction_FollowsGraceLinearDropAndFloor(double waitingSeconds, double expected)
        {
            double actual = WaveSummary.CalculateUnitSatisfaction(
                waitingSeconds,
                UtilConfig.getPatienceGrace(),
                UtilConfig.getPatienceFloorAfter(),
                UtilConfig.getWaveFloor());

            Assert.AreEqual(expected, actual, 1e-6);
        }

        [Test]
        public void WaveSatisfaction_WeightsOceanViewMoreThanOtherNeeds()
        {

            var waitingTimes = new Dictionary<NeedType, IReadOnlyList<UnitWaitingTime>>
            {
                { NeedType.Rooms, Waits(0.0) },
                { NeedType.OceanView, Waits(30.0) }
            };

            Assert.AreEqual(55, WaveSummary.CalculateWaveSatisfaction(waitingTimes));
        }

        [Test]
        public void WaveSatisfaction_ClampsToWaveFloorWhenAverageDropsBelow()
        {
            var waitingTimes = new Dictionary<NeedType, IReadOnlyList<UnitWaitingTime>>
            {
                { NeedType.Rooms, Waits(60.0, 60.0) },
                { NeedType.Food, Waits(60.0) }
            };

            Assert.AreEqual(40, WaveSummary.CalculateWaveSatisfaction(waitingTimes));
        }

        [Test]
        public void WaveSatisfaction_EmptyWaveReturnsWaveFloor()
        {
            var empty = new Dictionary<NeedType, IReadOnlyList<UnitWaitingTime>>();

            Assert.AreEqual(40, WaveSummary.CalculateWaveSatisfaction(empty));
        }

        [Test]
        public void WaveSatisfaction_NullInputThrows()
        {
            Assert.Throws<ArgumentNullException>(() => WaveSummary.CalculateWaveSatisfaction(null));
        }

    }
}
