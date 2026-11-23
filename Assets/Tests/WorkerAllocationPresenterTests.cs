using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class WorkerAllocationPresenterTests
    {
        private static readonly Func<string, string, bool> AlwaysCanMove = (from, to) => true;

        private static Dictionary<string, NeedType> ThreeNeeds() => new()
        {
            { "Reception", NeedType.Rooms },
            { "Restaurant", NeedType.Food },
            { "BeachBar", NeedType.Drinks }
        };

        // Reception and Restaurant staffed, BeachBar empty (start of the prototype).
        private static Dictionary<string, string> StartAssignments() => new()
        {
            { "Reception", "Worker1" },
            { "Restaurant", "Worker2" },
            { "BeachBar", null }
        };

        [Test]
        public void Build_EmptyTarget_StaffedBuildingsCanBeSelected()
        {
            WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
                "BeachBar", StartAssignments(), ThreeNeeds(), AlwaysCanMove);

            Assert.IsFalse(data.TargetHasWorker);
            Assert.AreEqual(NeedType.Drinks, data.TargetNeed);
            Assert.AreEqual(2, data.Cards.Count);
            Assert.IsTrue(data.Cards[0].CanSelect);
            Assert.IsTrue(data.Cards[1].CanSelect);
        }

        [Test]
        public void Build_CardsSkipTargetAndFollowS2Order()
        {
            WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
                "Reception", StartAssignments(), ThreeNeeds(), AlwaysCanMove);

            Assert.AreEqual(2, data.Cards.Count);
            Assert.AreEqual("BeachBar", data.Cards[0].BuildingId);
            Assert.AreEqual("Restaurant", data.Cards[1].BuildingId);
        }

        [Test]
        public void Build_StaffedTarget_OffersNoMove()
        {
            WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
                "Restaurant", StartAssignments(), ThreeNeeds(), AlwaysCanMove);

            Assert.IsTrue(data.TargetHasWorker);

            foreach (BuildingCardData card in data.Cards)
                Assert.IsFalse(card.CanSelect, card.BuildingId);
        }

        [Test]
        public void Build_CardsShowOccupancy()
        {
            WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
                "Restaurant", StartAssignments(), ThreeNeeds(), AlwaysCanMove);

            Assert.AreEqual("Reception", data.Cards[0].BuildingId);
            Assert.IsTrue(data.Cards[0].HasWorker);
            Assert.AreEqual("BeachBar", data.Cards[1].BuildingId);
            Assert.IsFalse(data.Cards[1].HasWorker);
        }

        [Test]
        public void Build_RunnerRefusesMove_CardCannotBeSelected()
        {
            WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
                "BeachBar", StartAssignments(), ThreeNeeds(), (from, to) => from != "Reception");

            Assert.AreEqual("Reception", data.Cards[0].BuildingId);
            Assert.IsFalse(data.Cards[0].CanSelect);
            Assert.IsTrue(data.Cards[1].CanSelect);
        }

        [Test]
        public void Build_CanMoveGetsSourceAndTarget()
        {
            var calls = new List<string>();

            WorkerAllocationPresenter.Build("BeachBar", StartAssignments(), ThreeNeeds(), (from, to) =>
            {
                calls.Add($"{from}->{to}");
                return true;
            });

            CollectionAssert.AreEquivalent(new[] { "Reception->BeachBar", "Restaurant->BeachBar" }, calls);
        }

        [Test]
        public void Build_FiveBuildings_ReturnsMaxCards()
        {
            var needs = ThreeNeeds();
            needs.Add("Pool", NeedType.Relax);
            needs.Add("Bungalows", NeedType.OceanView);

            var assignments = StartAssignments();
            assignments.Add("Pool", "Worker3");
            assignments.Add("Bungalows", "Worker4");

            WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
                "BeachBar", assignments, needs, AlwaysCanMove);

            Assert.AreEqual(WorkerAllocationPresenter.MaxCards, data.Cards.Count);
            Assert.AreEqual("Bungalows", data.Cards[3].BuildingId);
        }

        [Test]
        public void Build_UnknownTarget_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                WorkerAllocationPresenter.Build("Pool", StartAssignments(), ThreeNeeds(), AlwaysCanMove));
        }

        [Test]
        public void Build_BuildingWithoutNeed_Throws()
        {
            var needs = ThreeNeeds();
            needs.Remove("Restaurant");

            Assert.Throws<ArgumentException>(() =>
                WorkerAllocationPresenter.Build("BeachBar", StartAssignments(), needs, AlwaysCanMove));
        }

        [Test]
        public void Build_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() =>
                WorkerAllocationPresenter.Build(null, StartAssignments(), ThreeNeeds(), AlwaysCanMove));
            Assert.Throws<ArgumentNullException>(() =>
                WorkerAllocationPresenter.Build("BeachBar", null, ThreeNeeds(), AlwaysCanMove));
            Assert.Throws<ArgumentNullException>(() =>
                WorkerAllocationPresenter.Build("BeachBar", StartAssignments(), null, AlwaysCanMove));
            Assert.Throws<ArgumentNullException>(() =>
                WorkerAllocationPresenter.Build("BeachBar", StartAssignments(), ThreeNeeds(), null));
        }

        [Test]
        public void BuildingCardData_SelectableWithoutWorker_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new BuildingCardData("BeachBar", NeedType.Drinks, false, true));
        }
    }
}
