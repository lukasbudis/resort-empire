using System;
using ResortEmpire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GuestMessageText
{
    public NeedType need;
    public string complaint;
    public string praise;
}

// S2 footer: guest message on BOTTLENECK and RECOVERING (FR-37). Texts are in English (NFR-06).
// Lives in NeedsPanel/Content/Footer, so it is enabled only while a wave runs.
// The bubble is hidden through a CanvasGroup, so the panel layout never moves.
public class GuestMessageBubble : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private CanvasGroup bubbleGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0.5f)] private float displaySeconds = GuestMessageFeed.DefaultDisplaySeconds;

    [Header("Farba pozadia podľa typu hlášky")]
    [SerializeField] private Image background;
    [SerializeField] private Color complaintColor = new(1f, 0.93f, 0.93f);
    [SerializeField] private Color praiseColor = new(0.91f, 0.98f, 0.94f);

    [SerializeField] private GuestMessageText[] texts =
    {
        new() { need = NeedType.Rooms, complaint = "Check-in takes a bit too long.", praise = "Check-in is moving faster now!" },
        new() { need = NeedType.Drinks, complaint = "Drinks take a bit too long.", praise = "Drinks are coming faster now!" },
        new() { need = NeedType.Food, complaint = "Food takes a bit too long.", praise = "Food is coming faster now!" },
        new() { need = NeedType.Relax, complaint = "The pool queue is way too long.", praise = "The pool queue is getting shorter!" },
        new() { need = NeedType.OceanView, complaint = "The bungalows take a bit too long.", praise = "The bungalows are getting faster!" }
    };

    private GuestMessageFeed feed;

    private void Awake()
    {
        feed = new GuestMessageFeed(displaySeconds);
        feed.CurrentChanged += Show;
    }

    private void OnEnable()
    {
        feed.Clear();
        Show(null);

        if (runner == null)
        {
            Debug.LogError("GuestMessageBubble nemá priradený SimulationRunner.", this);
            return;
        }

        runner.NeedStateChanged += OnNeedStateChanged;
        runner.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        if (runner == null)
            return;

        runner.NeedStateChanged -= OnNeedStateChanged;
        runner.GameStateChanged -= OnGameStateChanged;
    }

    // Real time, so the message stays readable for the same time in fast mode.
    private void Update()
    {
        feed.Advance(Time.unscaledDeltaTime);
    }

    private void OnNeedStateChanged(NeedType need, NeedState state)
    {
        feed.OnNeedStateChanged(need, state);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.WaveInProgress)
            feed.Clear();
    }

    // Testovacie akcie: ukážu hlášku bez čakania na bottleneck (len v Play mode počas vlny).
    [ContextMenu("Test Complaint (Food)")]
    private void TestComplaint() => ShowTestMessage(NeedState.Bottleneck);

    [ContextMenu("Test Praise (Food)")]
    private void TestPraise() => ShowTestMessage(NeedState.Recovering);

    private void ShowTestMessage(NeedState state)
    {
        if (feed == null || !isActiveAndEnabled)
        {
            Debug.LogError("Hlášku možno otestovať len v Play mode počas vlny.", this);
            return;
        }

        feed.OnNeedStateChanged(NeedType.Food, state);
    }

    private void Show(GuestMessage? message)
    {
        if (bubbleGroup != null)
            bubbleGroup.alpha = message.HasValue ? 1f : 0f;

        if (!message.HasValue)
        {
            if (messageText != null)
                messageText.SetText(string.Empty);

            return;
        }

        GuestMessage value = message.Value;

        if (messageText != null)
            messageText.SetText($"\"{GetText(value)}\"");

        if (background != null)
            background.color = value.Kind == GuestMessageKind.Complaint ? complaintColor : praiseColor;
    }

    private string GetText(GuestMessage message)
    {
        foreach (GuestMessageText text in texts)
        {
            if (text == null || text.need != message.Need)
                continue;

            return message.Kind == GuestMessageKind.Complaint ? text.complaint : text.praise;
        }

        return message.Kind == GuestMessageKind.Complaint
            ? $"{message.Need} takes a bit too long."
            : $"{message.Need} is getting faster!";
    }
}
