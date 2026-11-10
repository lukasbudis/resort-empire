using System;
using System.Collections.Generic;

namespace ResortEmpire
{
    public class BuildingService
    {
        private const float TickSeconds = 0.1f;
        private const double CompletionTolerance = 1e-12;

        private readonly Queue<(int id, long arrivalTick)> waitingUnits = new();
        private readonly List<UnitWaitingTime> waitingTimes = new();
        private readonly int[] activeUnitIds;
        private readonly double[] completedWork;
        private float serviceSeconds;
        private double serviceTicks;
        private int nextUnitId = 1;

        public long ElapsedTicks { get; private set; }
        public IReadOnlyList<UnitWaitingTime> WaitingTimes { get; }

        public int TotalArrived { get; private set; }
        public int TotalServed { get; private set; }
        public int Capacity => activeUnitIds.Length;
        public int WaitingCount => waitingUnits.Count;
        public int ActiveCount { get; private set; }
        public bool IsEmpty => WaitingCount == 0 && ActiveCount == 0;
        public float PerformanceMultiplier { get; private set; }

        // Rychlost zodpoveda skutocnej obsluhe zaokruhlenej na cele tiky (FR-09).
        public double ServiceRatePerTick => Capacity / serviceTicks;

        public BuildingService(int capacity, float serviceSeconds, float performanceMultiplier = 1f)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            serviceTicks = CalculateServiceTicks(serviceSeconds, performanceMultiplier);
            this.serviceSeconds = serviceSeconds;
            PerformanceMultiplier = performanceMultiplier;
            activeUnitIds = new int[capacity];
            completedWork = new double[capacity];
            WaitingTimes = waitingTimes.AsReadOnly();
        }

        // Zmena vykonu zachova uz vykonanu pracu vsetkych sediacich jednotiek.
        // Setter neposuva simulacny cas; novy vykon sa pouzije v nasledujucom Step.
        public void SetPerformanceMultiplier(float performanceMultiplier)
        {
            double newServiceTicks = CalculateServiceTicks(serviceSeconds, performanceMultiplier);
            PerformanceMultiplier = performanceMultiplier;
            serviceTicks = newServiceTicks;
        }

        public void AddArrivals(int count) => AddArrivalsAtTick(count, ElapsedTicks);

        // WaveModel hlasi prichody noveho kroku este pred BuildingService.Step.
        // Prichod aj usadenie v tom istom kroku musia mat rovnaky cas, teda cakanie 0.
        internal void AddArrivalsForStep(int count) => AddArrivalsAtTick(count, ElapsedTicks + 1);

        private void AddArrivalsAtTick(int count, long arrivalTick)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
                waitingUnits.Enqueue((nextUnitId++, arrivalTick));

            TotalArrived += count;
            FillFreeSlots(arrivalTick);
        }
        public void Step()
        {
            ElapsedTicks++;
            for (int i = 0; i < Capacity; i++)
            {
                if (activeUnitIds[i] == 0)
                    continue;

                // Jednotka dokonci obsluhu pri podiele 1; zmena vykonu ju nerestartuje.
                completedWork[i] += 1.0 / serviceTicks;
                if (completedWork[i] >= 1.0 - CompletionTolerance)
                {
                    activeUnitIds[i] = 0;
                    completedWork[i] = 0;
                    ActiveCount--;
                    TotalServed++;
                }
            }

            // Novousadene jednotky zacnu postupovat az v dalsom kroku, ako doteraz.
            FillFreeSlots(ElapsedTicks);
        }

        public int GetActiveUnitId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Capacity)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            return activeUnitIds[slotIndex];
        }

        private void FillFreeSlots(long seatedTick)
        {
            for (int i = 0; i < Capacity && waitingUnits.Count > 0; i++)
            {
                if (activeUnitIds[i] != 0)
                    continue;

                var unit = waitingUnits.Dequeue();
                activeUnitIds[i] = unit.id;
                // Meriame iba rad: od prichodu po obsadenie miesta, nie cas obsluhy.
                waitingTimes.Add(new UnitWaitingTime(unit.id, unit.arrivalTick, seatedTick));
                completedWork[i] = 0;
                ActiveCount++;
            }
        }

        // Fast mode meni cas obsluhy aj vykon bez straty rozbehnuteho priebehu.
        // Zachovava povodne verejne rozhranie pouzivane GameSimulation.
        public void UpdateFastMode(float serviceSeconds, float performanceMultiplier = 1f)
        {
            double newServiceTicks = CalculateServiceTicks(serviceSeconds, performanceMultiplier);
            this.serviceSeconds = serviceSeconds;
            PerformanceMultiplier = performanceMultiplier;
            serviceTicks = newServiceTicks;
        }

        private static double CalculateServiceTicks(float serviceSeconds, float performanceMultiplier)
        {
            if (float.IsNaN(serviceSeconds) || float.IsInfinity(serviceSeconds) || serviceSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(serviceSeconds));
            if (float.IsNaN(performanceMultiplier) || float.IsInfinity(performanceMultiplier) || performanceMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(performanceMultiplier));

            // Tolerancia odstrani iba sum floatov na hranici celeho tiku (SCRUM-115).
            // Double zabrani preteceniu pri velmi pomalej, ale kladnej obsluhe.
            return Math.Max(1.0, Math.Ceiling(
                (double)serviceSeconds / (TickSeconds * (double)performanceMultiplier) - 1e-6));
        }
    }
}
