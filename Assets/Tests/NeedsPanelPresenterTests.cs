using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class NeedsPanelPresenterTests
    {
        private static readonly Dictionary<NeedType, int> NoServed = new();
        private static readonly Dictionary<NeedType, NeedState> NoStates = new();

        private static Dictionary<NeedType, int> ThreeNeeds() => new()
        {
            { NeedType.Food, 6 },
            { NeedType.Rooms, 8 },
            { NeedType.Drinks, 8 }
        };

        private static Dictionary<NeedType, int> FiveNeeds() => new()
        {
            { NeedType.OceanView, 10 },
            { NeedType.Food, 22 },
            { NeedType.Relax, 20 },
            { NeedType.Rooms, 25 },
            { NeedType.Drinks, 28 }
        };

        [Test]
        public void Build_ThreeNeeds_ReturnsRowsInDisplayOrder()
        {
            NeedsPanelData data = NeedsPanelPresenter.Build(1, ThreeNeeds(), NoServed, NoStates);

            Assert.AreEqual(3, data.Rows.Count);
            Assert.AreEqual(NeedType.Rooms, data.Rows[0].Need);
            Assert.AreEqual(NeedType.Drinks, data.Rows[1].Need);
            Assert.AreEqual(NeedType.Food, data.Rows[2].Need);
        }

        [Test]
        public void Build_FiveNeeds_ReturnsFiveRowsWithOceanViewLast()
        {
            NeedsPanelData data = NeedsPanelPresenter.Build(7, FiveNeeds(), NoServed, NoStates);

            Assert.AreEqual(NeedsPanelPresenter.MaxRows, data.Rows.Count);
            Assert.AreEqual(NeedType.Relax, data.Rows[3].Need);
            Assert.AreEqual(NeedType.OceanView, data.Rows[4].Need);
        }

        [Test]
        public void Build_OnlyOceanViewIsPremium()
        {
            NeedsPanelData data = NeedsPanelPresenter.Build(7, FiveNeeds(), NoServed, NoStates);

            foreach (NeedRowData row in data.Rows)
                Assert.AreEqual(row.Need == NeedType.OceanView, row.IsPremium, row.Need.ToString());
        }

        [Test]
        public void Build_CopiesWaveIdServedAndTotal()
        {
            var served = new Dictionary<NeedType, int> { { NeedType.Food, 2 } };

            NeedsPanelData data = NeedsPanelPresenter.Build(3, ThreeNeeds(), served, NoStates);
            NeedRowData food = data.Rows[2];

            Assert.AreEqual(3, data.WaveId);
            Assert.AreEqual(2, food.Served);
            Assert.AreEqual(6, food.Total);
        }

        [Test]
        public void Build_MissingServedAndState_DefaultsToZeroAndInProgress()
        {
            NeedsPanelData data = NeedsPanelPresenter.Build(1, ThreeNeeds(), NoServed, NoStates);

            foreach (NeedRowData row in data.Rows)
            {
                Assert.AreEqual(0, row.Served);
                Assert.AreEqual(NeedRowStatus.InProgress, row.Status);
            }
        }

        [Test]
        public void Build_UsesStateOfEachNeed()
        {
            var states = new Dictionary<NeedType, NeedState>
            {
                { NeedType.Rooms, NeedState.Satisfied },
                { NeedType.Drinks, NeedState.Ok },
                { NeedType.Food, NeedState.Bottleneck }
            };

            NeedsPanelData data = NeedsPanelPresenter.Build(1, ThreeNeeds(), NoServed, states);

            Assert.AreEqual(NeedRowStatus.Done, data.Rows[0].Status);
            Assert.AreEqual(NeedRowStatus.InProgress, data.Rows[1].Status);
            Assert.AreEqual(NeedRowStatus.Bottleneck, data.Rows[2].Status);
        }

        [Test]
        public void Build_EmptyDemand_ReturnsNoRows()
        {
            NeedsPanelData data = NeedsPanelPresenter.Build(
                1, new Dictionary<NeedType, int>(), NoServed, NoStates);

            Assert.AreEqual(0, data.Rows.Count);
        }

        [Test]
        public void Build_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                NeedsPanelPresenter.Build(1, null, NoServed, NoStates));
            Assert.Throws<ArgumentNullException>(() =>
                NeedsPanelPresenter.Build(1, ThreeNeeds(), null, NoStates));
            Assert.Throws<ArgumentNullException>(() =>
                NeedsPanelPresenter.Build(1, ThreeNeeds(), NoServed, null));
        }

        [Test]
        public void DisplayOrder_ContainsEveryNeedTypeOnce()
        {
            NeedType[] all = (NeedType[])Enum.GetValues(typeof(NeedType));

            Assert.AreEqual(all.Length, NeedsPanelPresenter.DisplayOrder.Count);
            CollectionAssert.AllItemsAreUnique(NeedsPanelPresenter.DisplayOrder);
            CollectionAssert.AreEquivalent(all, NeedsPanelPresenter.DisplayOrder);
        }

        [Test]
        public void DisplayOrder_FitsIntoPanelSlots()
        {
            Assert.LessOrEqual(NeedsPanelPresenter.DisplayOrder.Count, NeedsPanelPresenter.MaxRows);
        }

        [TestCase(NeedState.Ok, NeedRowStatus.InProgress)]
        [TestCase(NeedState.Bottleneck, NeedRowStatus.Bottleneck)]
        [TestCase(NeedState.Recovering, NeedRowStatus.Recovering)]
        [TestCase(NeedState.Satisfied, NeedRowStatus.Done)]
        public void ToStatus_MapsEveryNeedState(NeedState state, NeedRowStatus expected)
        {
            Assert.AreEqual(expected, NeedsPanelPresenter.ToStatus(state));
        }

        [TestCase(0, 8, 0f)]
        [TestCase(4, 8, 0.5f)]
        [TestCase(8, 8, 1f)]
        [TestCase(9, 8, 1f)]
        [TestCase(0, 0, 0f)]
        public void Progress_IsServedOverTotalClampedToOne(int served, int total, float expected)
        {
            var row = new NeedRowData(NeedType.Food, served, total, NeedRowStatus.InProgress);

            Assert.AreEqual(expected, row.Progress, 0.0001f);
        }

        [Test]
        public void NeedRowData_NegativeCounts_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new NeedRowData(NeedType.Food, -1, 6, NeedRowStatus.InProgress));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new NeedRowData(NeedType.Food, 0, -1, NeedRowStatus.InProgress));
        }
    }
}
