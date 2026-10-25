using System;
using TMPro;
using UnityEngine;

public class HudController : MonoBehaviour
{
    public GameObject coinsText;
    public GameObject energyText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UtilConfig.loadConfig(); // Toto presunut do nejakej Load metody co sa spustí na začiatku. Teraz ešte taká metóda asi neexistuje
        coinsText.GetComponent<TMP_Text>().SetText(UtilConfig.getCoins().ToString());
        energyText.GetComponent<TMP_Text>().SetText(UtilConfig.getCurrentEnergy().ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
