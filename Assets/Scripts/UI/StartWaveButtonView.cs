using ResortEmpire;
using UnityEngine;
using UnityEngine.UI;

public class StartWaveButtonView : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private GameObject buttonRoot;
    [SerializeField] private Button startWaveButton;

    private void OnEnable()
    {
        if (runner == null)
        {
            Debug.LogError("StartWaveButtonView nemá priradený SimulationRunner.", this);
            return;
        }

        runner.GameStateChanged += HandleGameStateChanged;
        ApplyState(runner.CurrentState);
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.GameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        ApplyState(state);
    }

    private void ApplyState(GameState state)
    {
        bool canStartWave = state == GameState.AwaitingWave;

        if (buttonRoot != null)
            buttonRoot.SetActive(canStartWave);

        if (startWaveButton != null)
            startWaveButton.interactable = canStartWave;
    }
}