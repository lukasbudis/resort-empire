using System.Text;
using ResortEmpire;
using TMPro;
using UnityEngine;

public class WaveForecastPanel : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text rowsText;

    private void OnEnable()
    {
        if (runner == null)
        {
            Debug.LogError("WaveForecastPanel nemá priradený SimulationRunner.", this);
            return;
        }

        runner.GameStateChanged += OnGameStateChanged;
        Refresh();
    }

    private void Start()
    {
        // Obnoví počiatočný forecast po inicializácii SimulationRunner.Awake().
        Refresh();
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.GameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (runner == null)
            return;

        WaveForecast forecast = runner.GetNextWaveForecast();

        bool shouldShow =
            runner.CurrentState == GameState.AwaitingWave &&
            forecast != null &&
            forecast.Rows.Count > 0;

        if (content != null)
            content.SetActive(shouldShow);

        if (!shouldShow)
            return;

        if (headerText != null)
            headerText.SetText($"NEXT WAVE {forecast.WaveId}");

        if (rowsText != null)
        {
            var text = new StringBuilder();

            foreach (WaveForecastRow row in forecast.Rows)
                text.AppendLine($"{GetNeedLabel(row.Need)}: {row.Volume}");

            rowsText.SetText(text.ToString().TrimEnd());
        }
    }

    private static string GetNeedLabel(NeedType need)
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
