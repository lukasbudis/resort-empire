using System.Linq;
using ResortEmpire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveCompleteScreenView : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text satisfactionText;
    [SerializeField] private TMP_Text servedUnitsText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text wentWellText;
    [SerializeField] private TMP_Text needsImprovementText;
    [SerializeField] private GameObject[] stars;
    [SerializeField] private Button claimButton;
    [SerializeField] private WaveRatingStarsView ratingStarsView;

    private void Awake()
    {

        if (content != null)
            content.SetActive(false);
    }

    private void OnEnable()
    {
        if (runner != null)
            runner.WaveCompleted += Show;

        if (claimButton != null)
            claimButton.onClick.AddListener(ClaimRewards);
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.WaveCompleted -= Show;

        if (claimButton != null)
            claimButton.onClick.RemoveListener(ClaimRewards);
    }

    private void Show(int waveId)
    {
        WaveCompleteResult result = runner.LastWaveResult;
        

        if (result == null || result.WaveId != waveId)
            return;

        if (titleText != null)
            titleText.SetText($"WAVE {result.WaveId} COMPLETE!");

        if (satisfactionText != null)
            satisfactionText.SetText($"{result.SatisfactionPercent}%");

        if (servedUnitsText != null)
            servedUnitsText.SetText($"{result.ServedUnits}/{result.TotalUnits}");

        if (coinsText != null)
            coinsText.SetText($"+{result.CoinsReward} Coins");

        if (wentWellText != null)
        {
            wentWellText.SetText(result.Summary.WentWell.Count > 0
                ? $"{string.Join(" & ", result.Summary.WentWell.Select(NeedLabel))} went well!"
                : "No needs went well.");
        }

        if (needsImprovementText != null)
        {
            needsImprovementText.SetText(result.Summary.NeedsImprovement.HasValue
                ? $"{NeedLabel(result.Summary.NeedsImprovement.Value)} needs improvement."
                : "No bottlenecks this wave!");
        }

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                    stars[i].SetActive(i < result.Rating);
            }
        }

        if (content == null)
        {
            Debug.LogError("WaveCompleteScreenView nemá priradený Content.", this);
            return;
        }

        if (content != null)
                content.SetActive(true);
        

        
        content.SetActive(true);
        Debug.Log($"S4 zobrazuje výsledok vlny {result.WaveId}.", this);
        ratingStarsView.DisplayRating(result.Rating);
    }

        private void ClaimRewards()
        {
            if (!runner.ClaimWaveRewards())
                return;

            if (content != null)
                content.SetActive(false);

        }

    private static string NeedLabel(NeedType need)
    {
        switch (need)
        {
            case NeedType.Rooms: return "Rooms";
            case NeedType.Drinks: return "Drinks";
            case NeedType.Food: return "Food";
            case NeedType.Relax: return "Relax";
            case NeedType.OceanView: return "Ocean View";
            default: return need.ToString();
        }
    }
}