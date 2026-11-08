using ResortEmpire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One need row in S2 (FR-36). Only displays NeedRowData, all decisions are in NeedsPanelPresenter.
// The row has a fixed height; nothing here is switched with SetActive, so the layout never moves.
public class NeedRow : MonoBehaviour
{
    [Header("Obsah")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text statusText;

    // Image Type = Filled, Fill Method = Horizontal.
    [SerializeField] private Image progressFill;

    [Header("Varovanie pri bottlenecku (FR-37)")]
    [SerializeField] private CanvasGroup warningGroup;
    [SerializeField] private TMP_Text warningText;

    [Header("Rám a prémiový need (Ocean View)")]
    [SerializeField] private Image frame;
    [SerializeField] private CanvasGroup premiumDecor;
    [SerializeField] private Color frameColor = new(0.36f, 0.20f, 0.11f);
    [SerializeField] private Color bottleneckFrameColor = new(0.94f, 0.27f, 0.27f);
    [SerializeField] private Color premiumFrameColor = new(0.88f, 0.65f, 0.15f);

    [Header("Farby stavov")]
    [SerializeField] private Color inProgressColor = new(0.96f, 0.62f, 0.04f);
    [SerializeField] private Color bottleneckColor = new(0.94f, 0.27f, 0.27f);
    [SerializeField] private Color recoveringColor = new(0.08f, 0.72f, 0.65f);
    [SerializeField] private Color doneColor = new(0.06f, 0.73f, 0.51f);

    [Header("Texty stavov (NFR-06: anglicky)")]
    [SerializeField] private string bottleneckLabel = "BOTTLENECK";
    [SerializeField] private string recoveringLabel = "RECOVERING";

    public void Bind(NeedRowData data, NeedVisual visual)
    {
        Color color = GetColor(data.Status);

        if (icon != null)
        {
            icon.sprite = visual?.icon;
            icon.color = color;
        }

        if (titleText != null)
            titleText.SetText(visual != null ? visual.title : data.Need.ToString());

        if (countText != null)
        {
            countText.SetText($"{data.Served}/{data.Total}");
            countText.color = color;
        }

        if (statusText != null)
        {
            statusText.SetText(GetLabel(data.Status));
            statusText.color = color;
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = data.Progress;
            progressFill.color = color;
        }

        bool showWarning = data.Status == NeedRowStatus.Bottleneck;

        if (warningGroup != null)
            warningGroup.alpha = showWarning ? 1f : 0f;

        if (warningText != null)
            warningText.SetText(showWarning && visual != null ? visual.bottleneckWarning : string.Empty);

        if (premiumDecor != null)
            premiumDecor.alpha = data.IsPremium ? 1f : 0f;

        if (frame != null)
            frame.color = GetFrameColor(data);
    }

    private Color GetColor(NeedRowStatus status)
    {
        switch (status)
        {
            case NeedRowStatus.Bottleneck: return bottleneckColor;
            case NeedRowStatus.Recovering: return recoveringColor;
            case NeedRowStatus.Done: return doneColor;
            default: return inProgressColor;
        }
    }

    private string GetLabel(NeedRowStatus status)
    {
        switch (status)
        {
            case NeedRowStatus.Bottleneck: return bottleneckLabel;
            case NeedRowStatus.Recovering: return recoveringLabel;
            default: return string.Empty;
        }
    }

    // Bottleneck highlight wins over the premium look (FR-37).
    private Color GetFrameColor(NeedRowData data)
    {
        if (data.Status == NeedRowStatus.Bottleneck)
            return bottleneckFrameColor;

        return data.IsPremium ? premiumFrameColor : frameColor;
    }
}
