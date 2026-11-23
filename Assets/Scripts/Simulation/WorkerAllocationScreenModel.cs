using System;
using System.Collections.Generic;

namespace ResortEmpire
{
    // One building card in S3: a building other than the target, with its occupancy (FR-16).
    public readonly struct BuildingCardData
    {
        public string BuildingId { get; }
        public NeedType Need { get; }
        public bool HasWorker { get; }

        // True when the player can take this building's Worker to the target;
        // this building is then left empty (FR-11, FR-37).
        public bool CanSelect { get; }

        public BuildingCardData(string buildingId, NeedType need, bool hasWorker, bool canSelect)
        {
            if (string.IsNullOrWhiteSpace(buildingId))
                throw new ArgumentException("Budova musí mať ID.", nameof(buildingId));

            if (canSelect && !hasWorker)
                throw new ArgumentException("Vybrať sa dá len budova s Workerom.", nameof(canSelect));

            BuildingId = buildingId;
            Need = need;
            HasWorker = hasWorker;
            CanSelect = canSelect;
        }
    }

    public sealed class WorkerAllocationScreenData
    {
        public string TargetBuildingId { get; }
        public NeedType TargetNeed { get; }

        // FR-37: S3 shows that the target is staffed and offers no move to it.
        public bool TargetHasWorker { get; }
        public IReadOnlyList<BuildingCardData> Cards { get; }

        public WorkerAllocationScreenData(
            string targetBuildingId,
            NeedType targetNeed,
            bool targetHasWorker,
            IReadOnlyList<BuildingCardData> cards)
        {
            if (string.IsNullOrWhiteSpace(targetBuildingId))
                throw new ArgumentException("Cieľová budova musí mať ID.", nameof(targetBuildingId));

            TargetBuildingId = targetBuildingId;
            TargetNeed = targetNeed;
            TargetHasWorker = targetHasWorker;
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }
    }

    // Turns the Worker allocation into the content of S3 for one target building.
    public static class WorkerAllocationPresenter
    {
        // At most 5 buildings, the target is not a card.
        public const int MaxCards = 4;

        // assignments = live view from the runner (building -> Worker ID or null),
        // buildingNeeds = need of every available building,
        // canMove = runner.CanMoveWorker(from, to), so the runner's rules stay in one place.
        public static WorkerAllocationScreenData Build(
            string targetBuildingId,
            IReadOnlyDictionary<string, string> assignments,
            IReadOnlyDictionary<string, NeedType> buildingNeeds,
            Func<string, string, bool> canMove)
        {
            if (targetBuildingId == null)
                throw new ArgumentNullException(nameof(targetBuildingId));

            if (assignments == null)
                throw new ArgumentNullException(nameof(assignments));

            if (buildingNeeds == null)
                throw new ArgumentNullException(nameof(buildingNeeds));

            if (canMove == null)
                throw new ArgumentNullException(nameof(canMove));

            if (!assignments.TryGetValue(targetBuildingId, out string targetWorker))
                throw new ArgumentException("Cieľová budova nie je dostupná.", nameof(targetBuildingId));

            if (!buildingNeeds.TryGetValue(targetBuildingId, out NeedType targetNeed))
                throw new ArgumentException("Cieľová budova nemá need.", nameof(buildingNeeds));

            bool targetHasWorker = targetWorker != null;
            var cards = new List<BuildingCardData>(MaxCards);

            // Same order as the needs in S2, so the player finds buildings in the same place.
            foreach (NeedType need in NeedsPanelPresenter.DisplayOrder)
            {
                foreach (var pair in assignments)
                {
                    if (pair.Key == targetBuildingId)
                        continue;

                    if (!buildingNeeds.TryGetValue(pair.Key, out NeedType buildingNeed))
                        throw new ArgumentException($"Budova '{pair.Key}' nemá need.", nameof(buildingNeeds));

                    if (buildingNeed != need)
                        continue;

                    bool hasWorker = pair.Value != null;
                    bool canSelect = !targetHasWorker && hasWorker && canMove(pair.Key, targetBuildingId);
                    cards.Add(new BuildingCardData(pair.Key, buildingNeed, hasWorker, canSelect));
                }
            }

            if (cards.Count > MaxCards)
                throw new ArgumentException($"S3 zobrazí najviac {MaxCards} budovy okrem cieľovej.", nameof(assignments));

            return new WorkerAllocationScreenData(targetBuildingId, targetNeed, targetHasWorker, cards);
        }
    }
}
