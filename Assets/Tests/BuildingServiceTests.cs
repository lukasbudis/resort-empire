
using NUnit.Framework;

namespace ResortEmpire
{
    public class BuildingServiceTests
    {
        private static void StepMany(BuildingService service, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                service.Step();
        }

        [TestCase(0.5f, 1f, 4, 8)]
        [TestCase(1f, 0.5f, 4, 12)]
        [TestCase(1f, 2f, 4, 3)]
        [TestCase(2f, 1f, 2, 6)]
        [TestCase(0.33f, 1f, 10, 7)]
        [TestCase(1f, 0.33f, 4, 19)]
        public void PerformanceChangePreservesActiveProgress(
            float initial, float updated, int before, int after)
        {
            var service = new BuildingService(1, 1f, initial);
            service.AddArrivals(1);
            StepMany(service, before);
            service.SetPerformanceMultiplier(updated);

            Assert.AreEqual(updated, service.PerformanceMultiplier);
            Assert.AreEqual(0, service.TotalServed);
            Assert.AreEqual(1, service.GetActiveUnitId(0));
            StepMany(service, after - 1);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(1, service.TotalServed);
            Assert.IsTrue(service.IsEmpty);
        }

        [Test]
        public void RepeatedChangesPreserveWorkWithoutRoundingRemainder()
        {
            var service = new BuildingService(1, 1f, 0.5f);
            service.AddArrivals(1);
            StepMany(service, 2); // Hotovych 10 percent.
            service.SetPerformanceMultiplier(1f);
            StepMany(service, 2); // Hotovych 30 percent.
            service.SetPerformanceMultiplier(2f);
            service.Step(); // Hotovych 50 percent.
            service.SetPerformanceMultiplier(0.5f);
            service.SetPerformanceMultiplier(0.5f);
            StepMany(service, 9);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(1, service.TotalServed);
        }

        [Test]
        public void DifferentSeatsKeepTheirOwnProgressAndQueueRemainsFifo()
        {
            var service = new BuildingService(2, 1f);
            service.AddArrivals(1);
            StepMany(service, 4);
            service.AddArrivals(3);
            StepMany(service, 2);
            service.SetPerformanceMultiplier(0.5f);
            StepMany(service, 7);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(1, service.TotalServed);
            Assert.AreEqual(3, service.GetActiveUnitId(0));
            Assert.AreEqual(2, service.GetActiveUnitId(1));
            StepMany(service, 8);
            Assert.AreEqual(2, service.TotalServed);
            Assert.AreEqual(4, service.GetActiveUnitId(1));
            StepMany(service, 20);
            Assert.AreEqual(4, service.TotalArrived);
            Assert.AreEqual(4, service.TotalServed);
            Assert.IsTrue(service.IsEmpty);
        }

        [Test]
        public void PerformanceSetBeforeArrivalsAppliesToNewUnits()
        {
            var service = new BuildingService(1, 1f);
            service.SetPerformanceMultiplier(0.5f);
            service.AddArrivals(2);
            StepMany(service, 19);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(2, service.GetActiveUnitId(0));
            StepMany(service, 20);
            Assert.AreEqual(2, service.TotalServed);
        }

        [Test]
        public void RateChangesImmediatelyAndMatchesRoundedDuration()
        {
            var service = new BuildingService(2, 0.25f);
            Assert.AreEqual(2.0 / 3, service.ServiceRatePerTick, 1e-12);
            service.SetPerformanceMultiplier(0.5f);
            Assert.AreEqual(2.0 / 5, service.ServiceRatePerTick, 1e-12);
            service.AddArrivals(2);
            StepMany(service, 4);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(2, service.TotalServed);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidPerformanceLeavesActiveServiceUnchanged(float value)
        {
            var service = new BuildingService(1, 1f);
            service.AddArrivals(1);
            StepMany(service, 4);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => service.SetPerformanceMultiplier(value));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => service.UpdateFastMode(0.5f, value));
            Assert.AreEqual(1f, service.PerformanceMultiplier);
            Assert.AreEqual(0.1, service.ServiceRatePerTick, 1e-12);
            StepMany(service, 5);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(1, service.TotalServed);
        }

        [Test]
        public void FastModeKeepsProgressAndSetterUsesUpdatedDuration()
        {
            var service = new BuildingService(1, 2f);
            service.AddArrivals(1);
            StepMany(service, 4); // Hotovych 20 percent pri povodnom case.
            service.UpdateFastMode(1f, 0.5f);
            service.SetPerformanceMultiplier(1f);
            Assert.AreEqual(0.1, service.ServiceRatePerTick, 1e-12);
            StepMany(service, 7);
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(1, service.TotalServed);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidFastModeDurationLeavesParametersUnchanged(float value)
        {
            var service = new BuildingService(1, 1f);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => service.UpdateFastMode(value, 0.5f));
            Assert.AreEqual(1f, service.PerformanceMultiplier);
            service.SetPerformanceMultiplier(0.5f);
            Assert.AreEqual(0.05, service.ServiceRatePerTick, 1e-12);
        }
        [TestCase(0.3f, 1f, 3)]
        [TestCase(0.6f, 1f, 6)]
        [TestCase(1.1f, 1f, 11)]
        [TestCase(2.7f, 1f, 27)]
        [TestCase(0.31f, 1f, 4)]
        [TestCase(0.3001f, 1f, 4)]
        [TestCase(0.3f, 0.5f, 6)]
        [TestCase(0.6f, 2f, 3)]
        [TestCase(0.01f, 1f, 1)]
        public void ServiceCompletesOnExpectedTick(float seconds, float performance, int ticks)
        {
            var service = new BuildingService(1, seconds, performance);
            service.AddArrivals(1);
            for (int i = 0; i < ticks - 1; i++)
                service.Step();
            Assert.AreEqual(0, service.TotalServed);
            service.Step();
            Assert.AreEqual(1, service.TotalServed);
            Assert.IsTrue(service.IsEmpty);
        }
        [Test]
        public void ArrivalsOccupyFreeSlots()
        {
            var service = new BuildingService(2, 1f);

            service.AddArrivals(5);

            Assert.AreEqual(5, service.TotalArrived);
            Assert.AreEqual(2, service.ActiveCount);
            Assert.AreEqual(3, service.WaitingCount);

            Assert.AreEqual(1, service.GetActiveUnitId(0));
            Assert.AreEqual(2, service.GetActiveUnitId(1));
        }

        [Test]
        public void WaitingUnitsFollowFifoOrder()
        {
            var service = new BuildingService(1, 0.1f);

            service.AddArrivals(3);

            Assert.AreEqual(1, service.GetActiveUnitId(0));

            service.Step();
            Assert.AreEqual(2, service.GetActiveUnitId(0));

            service.Step();
            Assert.AreEqual(3, service.GetActiveUnitId(0));

            service.Step();

            Assert.AreEqual(3, service.TotalServed);
            Assert.IsTrue(service.IsEmpty);
        }

        [Test]
        public void ServiceRespectsConfiguredDuration()
        {
            var service = new BuildingService(1, 1f);

            service.AddArrivals(1);

            for (int i = 0; i < 9; i++)
                service.Step();

            Assert.AreEqual(0, service.TotalServed);

            service.Step();

            Assert.AreEqual(1, service.TotalServed);
            Assert.IsTrue(service.IsEmpty);
        }

        [Test]
        public void AllArrivedUnitsAreEventuallyServed()
        {
            var service = new BuildingService(2, 0.5f);

            service.AddArrivals(20);

            for (int i = 0; i < 100; i++)
                service.Step();

            Assert.AreEqual(20, service.TotalArrived);
            Assert.AreEqual(20, service.TotalServed);
            Assert.AreEqual(0, service.WaitingCount);
            Assert.AreEqual(0, service.ActiveCount);
            Assert.IsTrue(service.IsEmpty);
        }

        [Test]
        public void WorkerPerformanceChangesServiceSpeed()
        {
            var normal = new BuildingService(1, 1f, 1f);
            var slower = new BuildingService(1, 1f, 0.5f);

            normal.AddArrivals(1);
            slower.AddArrivals(1);

            for (int i = 0; i < 10; i++)
            {
                normal.Step();
                slower.Step();
            }

            Assert.AreEqual(1, normal.TotalServed);
            Assert.AreEqual(0, slower.TotalServed);

            for (int i = 0; i < 10; i++)
                slower.Step();

            Assert.AreEqual(1, slower.TotalServed);
        }
    }
}