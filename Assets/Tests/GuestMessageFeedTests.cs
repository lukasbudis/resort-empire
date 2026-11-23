using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class GuestMessageFeedTests
    {
        private static readonly GuestMessage FoodComplaint = new(NeedType.Food, GuestMessageKind.Complaint);
        private static readonly GuestMessage FoodPraise = new(NeedType.Food, GuestMessageKind.Praise);

        [Test]
        public void NewFeed_HasNoMessage()
        {
            var feed = new GuestMessageFeed();

            Assert.IsNull(feed.Current);
        }

        [Test]
        public void Bottleneck_ShowsComplaintForThatNeed()
        {
            var feed = new GuestMessageFeed();

            bool shown = feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);

            Assert.IsTrue(shown);
            Assert.AreEqual(FoodComplaint, feed.Current);
        }

        [Test]
        public void Recovering_ShowsPraiseForThatNeed()
        {
            var feed = new GuestMessageFeed();

            feed.OnNeedStateChanged(NeedType.Food, NeedState.Recovering);

            Assert.AreEqual(FoodPraise, feed.Current);
        }

        [Test]
        public void Recovering_ReplacesComplaintOfSameNeed()
        {
            var feed = new GuestMessageFeed();
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);

            feed.OnNeedStateChanged(NeedType.Food, NeedState.Recovering);

            Assert.AreEqual(FoodPraise, feed.Current);
        }

        [TestCase(NeedState.Ok)]
        [TestCase(NeedState.Satisfied)]
        public void OkOrSatisfied_ShowsNothing(NeedState state)
        {
            var feed = new GuestMessageFeed();

            bool shown = feed.OnNeedStateChanged(NeedType.Food, state);

            Assert.IsFalse(shown);
            Assert.IsNull(feed.Current);
        }

        [Test]
        public void OkAfterRecovering_KeepsPraise()
        {
            var feed = new GuestMessageFeed();
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Recovering);

            feed.OnNeedStateChanged(NeedType.Food, NeedState.Ok);

            Assert.AreEqual(FoodPraise, feed.Current);
        }

        [Test]
        public void NewerMessage_ReplacesOlderOfOtherNeed()
        {
            var feed = new GuestMessageFeed();
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);

            feed.OnNeedStateChanged(NeedType.Drinks, NeedState.Bottleneck);

            Assert.AreEqual(new GuestMessage(NeedType.Drinks, GuestMessageKind.Complaint), feed.Current);
        }

        [Test]
        public void Advance_BeforeDisplayTime_KeepsMessage()
        {
            var feed = new GuestMessageFeed(4f);
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);

            feed.Advance(3.9f);

            Assert.AreEqual(FoodComplaint, feed.Current);
            Assert.AreEqual(0.1f, feed.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void Advance_PastDisplayTime_HidesMessage()
        {
            var feed = new GuestMessageFeed(4f);
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);

            feed.Advance(2f);
            feed.Advance(2f);

            Assert.IsNull(feed.Current);
            Assert.AreEqual(0f, feed.RemainingSeconds);
        }

        [Test]
        public void NewMessage_RestartsDisplayTime()
        {
            var feed = new GuestMessageFeed(4f);
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);
            feed.Advance(3f);

            feed.OnNeedStateChanged(NeedType.Food, NeedState.Recovering);
            feed.Advance(3f);

            Assert.AreEqual(FoodPraise, feed.Current);
        }

        [Test]
        public void Clear_HidesMessage()
        {
            var feed = new GuestMessageFeed();
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);

            feed.Clear();

            Assert.IsNull(feed.Current);
        }

        [Test]
        public void CurrentChanged_RaisedOnShowReplaceAndHide()
        {
            var feed = new GuestMessageFeed(1f);
            var events = new List<GuestMessage?>();
            feed.CurrentChanged += message => events.Add(message);

            feed.OnNeedStateChanged(NeedType.Food, NeedState.Bottleneck);
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Ok);
            feed.OnNeedStateChanged(NeedType.Food, NeedState.Recovering);
            feed.Advance(1f);
            feed.Clear();

            Assert.AreEqual(3, events.Count);
            Assert.AreEqual(FoodComplaint, events[0]);
            Assert.AreEqual(FoodPraise, events[1]);
            Assert.IsNull(events[2]);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidDisplayTime_Throws(float seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GuestMessageFeed(seconds));
        }

        [Test]
        public void Advance_NegativeDelta_Throws()
        {
            var feed = new GuestMessageFeed();

            Assert.Throws<ArgumentOutOfRangeException>(() => feed.Advance(-0.1f));
        }
    }

    public class GuestMessageFeedIntegrationTests
    {
        [Test]
        public void SlowBuilding_ShowsComplaintThenPraise()
        {
            // 20 units in 10 s = 2/s against one seat served in 1 s = 1/s.
            var simulation = new GameSimulation(4);
            simulation.RegisterBuildingService(NeedType.Food, new BuildingService(1, 1f));
            simulation.StartWave(
                new Dictionary<NeedType, int> { { NeedType.Food, 20 } },
                new[] { NeedType.Food },
                10f);

            var feed = new GuestMessageFeed();
            var shown = new List<GuestMessage>();
            simulation.NeedStateChanged += (need, state) => feed.OnNeedStateChanged(need, state);
            feed.CurrentChanged += message =>
            {
                if (message.HasValue)
                    shown.Add(message.Value);
            };

            int ticks = 0;
            while (ticks < 10000 && !simulation.AllNeedsSatisfied)
            {
                simulation.Step();
                ticks++;
            }

            Assert.IsTrue(simulation.AllNeedsSatisfied, "The wave did not finish.");
            CollectionAssert.AreEqual(
                new[]
                {
                    new GuestMessage(NeedType.Food, GuestMessageKind.Complaint),
                    new GuestMessage(NeedType.Food, GuestMessageKind.Praise)
                },
                shown);
        }
    }
}
