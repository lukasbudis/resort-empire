using TMPro;
using UnityEngine;

public class ResourceHudView : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text resortValueText;

    public void Display(InitialGameState state)
    {
        if (state == null)
        {
            Debug.LogError("ResourceHudView: InitialGameState is null.", this);
            return;
        }

        if (coinsText == null || energyText == null || resortValueText == null)
        {
            Debug.LogError("ResourceHudView: Assign all three TMP text references in the Inspector.", this);
            return;
        }

        coinsText.text = state.coins.ToString();
        energyText.text = state.energy.ToString();
        resortValueText.text = state.resortValue.ToString();

        Debug.Log($"HUD updated: Coins={state.coins}, Energy={state.energy}, Resort Value={state.resortValue}.", this);
    }
}
