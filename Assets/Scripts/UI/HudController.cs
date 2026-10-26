using System;
using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public GameObject coinsText;
    public GameObject energyText;
    [SerializeField] private WorkerPlacement workerPlacement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UtilConfig.loadConfig(); // Toto presunut do nejakej Load metody co sa spustí na začiatku. Teraz ešte taká metóda asi neexistuje
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

    workerPlacement.Apply(config);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
