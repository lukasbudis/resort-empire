using System;
using ResortEmpire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BuildingVisual
{
    public string buildingId;
    public string title;
    public Sprite icon;
}

// One building card in S3. Only displays BuildingCardData, all decisions are in WorkerAllocationPresenter.
public class BuildingCard : MonoBehaviour
{
    [Header("Obsah")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button selectButton;

    [Header("Obsadenosť (FR-16)")]
    [SerializeField] private Image badge;
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private CanvasGroup cardGroup;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.6f;
    [SerializeField] private Color staffedBadgeColor = new(0.06f, 0.73f, 0.51f);
    [SerializeField] private Color emptyBadgeColor = new(0.88f, 0.65f, 0.15f);

    [Header("Texty (NFR-06: anglicky)")]
    [SerializeField] private string staffedLabel = "STAFFED";
    [SerializeField] private string emptyLabel = "EMPTY";
    [SerializeField] private string selectableInfo = "Will be left empty";
    [SerializeField] private string emptyInfo = "No Worker";
    [SerializeField] private string busyInfo = "";

    private string buildingId;

    public event Action<string> Selected;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    public void Bind(BuildingCardData data, BuildingVisual visual)
    {
        buildingId = data.BuildingId;

        if (icon != null)
            icon.sprite = visual?.icon;

        if (nameText != null)
            nameText.SetText(visual != null ? visual.title : data.BuildingId);

        if (infoText != null)
            infoText.SetText(GetInfo(data));

        if (badge != null)
            badge.color = data.HasWorker ? staffedBadgeColor : emptyBadgeColor;

        if (badgeText != null)
            badgeText.SetText(data.HasWorker ? staffedLabel : emptyLabel);

        // The button keeps its place in the card, it is only hidden.
        if (selectButton != null)
        {
            selectButton.interactable = data.CanSelect;
            selectButton.gameObject.SetActive(data.CanSelect);
        }

        if (cardGroup != null)
            cardGroup.alpha = data.CanSelect ? 1f : disabledAlpha;
    }

    private string GetInfo(BuildingCardData data)
    {
        if (data.CanSelect)
            return selectableInfo;

        return data.HasWorker ? busyInfo : emptyInfo;
    }

    private void OnSelectClicked()
    {
        if (buildingId != null)
            Selected?.Invoke(buildingId);
    }
}
