using System;
using System.Collections.Generic;
using ResortEmpire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// S3 Worker Allocation. Opens for a target building: from S2 by tapping a need (FR-37),
// before the wave by tapping a building on the map. The player picks the building that is left empty.
// S3 is an overlay on top of S1/S2, so closing it returns where it was opened from (FR-35).
// This component sits on an always active object; only `content` is shown or hidden.
public class WorkerAllocationScreen : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private GameObject content;

    [Header("Hlavička")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string titleFormat = "ASSIGN STAFF - {0}";
    [SerializeField] private Button closeButton;

    // Optional: tapping the dimmed background also closes S3.
    [SerializeField] private Button dimButton;

    [Header("Obsadená cieľová budova (FR-37)")]
    [SerializeField] private GameObject staffedNotice;
    [SerializeField] private TMP_Text staffedNoticeText;
    [SerializeField] private string staffedNoticeFormat = "{0} already has a Worker.";

    // Exactly WorkerAllocationPresenter.MaxCards slots, placed in the prefab in display order.
    [Header("Karty budov")]
    [SerializeField] private BuildingCard[] cards = new BuildingCard[WorkerAllocationPresenter.MaxCards];
    [SerializeField] private BuildingVisual[] visuals =
    {
        new() { buildingId = "Reception", title = "Reception" },
        new() { buildingId = "Restaurant", title = "Restaurant" },
        new() { buildingId = "BeachBar", title = "Beach Bar" },
        new() { buildingId = "Pool", title = "Pool" },
        new() { buildingId = "Bungalows", title = "Bungalows" }
    };

    private string targetBuildingId;
    private GameState openedInState;

    public bool IsOpen => targetBuildingId != null;

    private void Awake()
    {
        if (content != null)
            content.SetActive(false);
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (dimButton != null)
            dimButton.onClick.AddListener(Close);

        foreach (BuildingCard card in cards)
        {
            if (card != null)
                card.Selected += OnCardSelected;
        }

        if (runner == null)
        {
            Debug.LogError("WorkerAllocationScreen nemá priradený SimulationRunner.", this);
            return;
        }
        UpdateUnstaffedVisuals();

        runner.GameStateChanged += OnGameStateChanged;
        runner.WorkerMoved += OnWorkerMoved;
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (dimButton != null)
            dimButton.onClick.RemoveListener(Close);

        foreach (BuildingCard card in cards)
        {
            if (card != null)
                card.Selected -= OnCardSelected;
        }

        if (runner == null)
            return;

        runner.GameStateChanged -= OnGameStateChanged;
        runner.WorkerMoved -= OnWorkerMoved;
    }

    private void OnValidate()
    {
        if (cards == null || cards.Length != WorkerAllocationPresenter.MaxCards)
            Debug.LogError($"WorkerAllocationScreen musí mať presne {WorkerAllocationPresenter.MaxCards} karty.", this);
    }

    // From S2: the need is translated to the building that serves it (FR-37).
    public void Open(NeedType need)
    {
        string buildingId = runner != null ? runner.GetBuildingIdForNeed(need) : null;

        if (buildingId == null)
        {
            Debug.Log($"Need {need} nemá dostupnú budovu, S3 sa neotvorí.");
            return;
        }

        Open(buildingId);
    }

    // From the map, or from code. A locked or unknown building does not open S3.
    public void Open(string buildingId)
    {
        if (runner == null || runner.WorkerAssignments == null)
        {
            Debug.LogError("Simulácia nie je pripravená, S3 sa neotvorí.", this);
            return;
        }

        if (runner.CurrentState == GameState.AllWavesCompleted)
            return;

        if (buildingId == null || !runner.WorkerAssignments.ContainsKey(buildingId))
        {
            Debug.Log($"Budova '{buildingId}' nie je dostupná, S3 sa neotvorí.");
            return;
        }

        targetBuildingId = buildingId;
        openedInState = runner.CurrentState;

        if (content != null)
            content.SetActive(true);

        Render();
    }

    // Back to S1 or S2: S3 only hides itself, the screen under it stays as it was (FR-35).
    public void Close()
    {
        targetBuildingId = null;

        if (content != null)
            content.SetActive(false);
    }

    // Wave started or ended while S3 was open: S3 closes and S4 can be shown (FR-35).
    private void OnGameStateChanged(GameState state)
    {
        if (IsOpen && state != openedInState)
            Close();
    }

    private void OnWorkerMoved(string workerId, string fromBuildingId, string toBuildingId)
    {
        UpdateUnstaffedVisuals();
        
        if (IsOpen)
            Render();
    }

    private void OnCardSelected(string sourceBuildingId)
    {
        if (!IsOpen)
            return;

        // The state could change since the cards were drawn, so the result is checked.
        if (runner.TryMoveWorker(sourceBuildingId, targetBuildingId))
        {
            Debug.Log($"Worker presunutý z {sourceBuildingId} do {targetBuildingId}.");
            Close();
        }
        else
        {
            Debug.Log($"Presun z {sourceBuildingId} do {targetBuildingId} sa nedá vykonať.");
            Render();
        }
    }

    private void Render()
    {
        WorkerAllocationScreenData data = WorkerAllocationPresenter.Build(
            targetBuildingId,
            runner.WorkerAssignments,
            GetBuildingNeeds(),
            runner.CanMoveWorker);

        string targetTitle = GetTitle(data.TargetBuildingId);

        if (titleText != null)
            titleText.SetText(string.Format(titleFormat, targetTitle.ToUpperInvariant()));

        if (staffedNotice != null)
            staffedNotice.SetActive(data.TargetHasWorker);

        if (staffedNoticeText != null)
            staffedNoticeText.SetText(string.Format(staffedNoticeFormat, targetTitle));

        for (int i = 0; i < cards.Length; i++)
        {
            BuildingCard card = cards[i];

            if (card == null)
                continue;

            bool used = i < data.Cards.Count;

            if (card.gameObject.activeSelf != used)
                card.gameObject.SetActive(used);

            if (used)
                card.Bind(data.Cards[i], FindVisual(data.Cards[i].BuildingId));
        }
    }

    // The runner only exposes need -> building, so the map is built the other way here.
    private Dictionary<string, NeedType> GetBuildingNeeds()
    {
        var needs = new Dictionary<string, NeedType>(StringComparer.Ordinal);

        foreach (NeedType need in Enum.GetValues(typeof(NeedType)))
        {
            string buildingId = runner.GetBuildingIdForNeed(need);

            if (buildingId != null)
                needs[buildingId] = need;
        }

        return needs;
    }

    private string GetTitle(string buildingId)
    {
        BuildingVisual visual = FindVisual(buildingId);
        return visual != null && !string.IsNullOrEmpty(visual.title) ? visual.title : buildingId;
    }

    private BuildingVisual FindVisual(string buildingId)
    {
        foreach (BuildingVisual visual in visuals)
        {
            if (visual != null && visual.buildingId == buildingId)
                return visual;
        }

        return null;
    }

    private void UpdateUnstaffedVisuals()
    {
        if (runner == null || runner.WorkerAssignments == null)
            return;

        foreach (BuildingVisual visual in visuals)
        {
            if (visual == null || visual.noWorker == null)
                continue;

            // Zobrazí obrázok len pre dostupné budovy, ktoré aktuálne nemajú workera
            bool isAvailable = runner.WorkerAssignments.ContainsKey(visual.buildingId);
            bool hasWorker = runner.HasWorker(visual.buildingId);

            visual.noWorker.SetActive(isAvailable && !hasWorker);
        }
    }
}
