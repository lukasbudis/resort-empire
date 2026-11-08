using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire;

public class HudController : MonoBehaviour
{
    public GameObject coinsText;
    public GameObject energyText;
    public GameObject fastModeButton;
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private WorkerPlacement workerPlacement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MainConfig config = UtilConfig.getConfig();
        if (config == null || config.resourceConfig == null)
        {
            Debug.LogError("Konfigurácia nebola načítaná; HUD ani worker-i nebudú inicializovaní.");
            return;
        }

        coinsText.GetComponent<TMP_Text>().SetText(UtilConfig.getCoins().ToString());
        energyText.GetComponent<TMP_Text>().SetText(UtilConfig.getCurrentEnergy().ToString());

         if (workerPlacement == null)
        {
            Debug.LogError("WorkerPlacement nie je priradený v Inspectore.");
            return;
        }
        SetFastMode(false);
    workerPlacement.Apply(config);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FastModeButtonOnClick()
    {
        if (runner.CurrentState == GameState.AwaitingWave)
        {
            bool fastMode = UtilConfig.getFastMode();
            SetFastMode(!fastMode);    
        }
    }

    private void SetFastMode(bool mode)
    {
        UtilConfig.setFastMode(mode);
        runner.UpdateFastMode();
        if (mode)
        {
            Debug.Log("Fast mode ON.");
            fastModeButton.GetComponentInChildren<TMP_Text>().SetText("ON");
            var color = fastModeButton.GetComponent<Button>().colors;
            color.normalColor = Color.green;
            color.pressedColor = Color.green;
            color.selectedColor = Color.green;
            fastModeButton.GetComponent<Button>().colors = color;
        } else
        {
            Debug.Log("Fast mode OFF.");
            fastModeButton.GetComponentInChildren<TMP_Text>().SetText("OFF");
            var color = fastModeButton.GetComponent<Button>().colors;
            color.normalColor = Color.red;
            color.pressedColor = Color.red;
            color.selectedColor = Color.red;
            fastModeButton.GetComponent<Button>().colors = color;
        }    
    }
}
