using System;
using ResortEmpire;
using TMPro;
using UnityEngine;

[Serializable]
public class NeedVisual
{
    public NeedType need;
    public string title;
    public Sprite icon;
    public string bottleneckWarning;
}

// S2 panel: visible only while a wave runs, shows the needs of that wave (FR-35, FR-36).
// No satisfaction score during the wave (NFR-04).
// This component sits on an always active object; only `content` is shown or hidden,
// so the panel keeps listening to the runner while hidden.
public class NeedsPanel : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private string headerFormat = "WAVE {0} IN PROGRESS";

    // Exactly NeedsPanelPresenter.MaxRows slots, placed in the prefab in display order.
    [SerializeField] private NeedRow[] slots = new NeedRow[NeedsPanelPresenter.MaxRows];
    [SerializeField] private NeedVisual[] visuals =
    {
        new() { need = NeedType.Rooms, title = "Rooms", bottleneckWarning = "Reception Queue Growing" },
        new() { need = NeedType.Drinks, title = "Drinks", bottleneckWarning = "Beach Bar Queue Growing" },
        new() { need = NeedType.Food, title = "Food", bottleneckWarning = "Restaurant Queue Growing" },
        new() { need = NeedType.Relax, title = "Relax", bottleneckWarning = "Pool Queue Growing" },
        new() { need = NeedType.OceanView, title = "Ocean View", bottleneckWarning = "Bungalows Queue Growing" }
    };

    private bool dirty;

    private void OnEnable()
    {
        if (runner == null)
        {
            Debug.LogError("NeedsPanel nemá priradený SimulationRunner.", this);
            return;
        }

        runner.GameStateChanged += OnGameStateChanged;
        runner.Ticked += MarkDirty;

        OnGameStateChanged(runner.CurrentState);
    }

    private void OnDisable()
    {
        if (runner == null)
            return;

        runner.GameStateChanged -= OnGameStateChanged;
        runner.Ticked -= MarkDirty;
    }

    private void OnValidate()
    {
        if (slots == null || slots.Length != NeedsPanelPresenter.MaxRows)
            Debug.LogError($"NeedsPanel musí mať presne {NeedsPanelPresenter.MaxRows} slotov.", this);
    }

    private void OnGameStateChanged(GameState state)
    {
        bool waveActive = state == GameState.WaveInProgress;

        if (content != null)
            content.SetActive(waveActive);

        if (waveActive)
            Render();
        else
            dirty = false;
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    // Several ticks can run in one frame; the panel redraws at most once per frame.
    private void LateUpdate()
    {
        if (!dirty)
            return;

        dirty = false;
        Render();
    }

    private void Render()
    {
        NeedsPanelData data = runner != null ? runner.GetNeedsPanelData() : null;

        if (data == null)
            return;

        if (headerText != null)
            headerText.SetText(string.Format(headerFormat, data.WaveId));

        for (int i = 0; i < slots.Length; i++)
        {
            NeedRow slot = slots[i];

            if (slot == null)
                continue;

            bool used = i < data.Rows.Count;

            // Rows sit at the top of a fixed-height container, so hiding a slot
            // never moves the header or the footer.
            if (slot.gameObject.activeSelf != used)
                slot.gameObject.SetActive(used);

            if (used)
                slot.Bind(data.Rows[i], FindVisual(data.Rows[i].Need));
        }
    }

    private NeedVisual FindVisual(NeedType need)
    {
        foreach (NeedVisual visual in visuals)
        {
            if (visual != null && visual.need == need)
                return visual;
        }

        return null;
    }
}
