using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveRatingStarsView : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = new Color(1f, 0.75f, 0.1f);
    [SerializeField] private TMP_Text perfectText;

    public void DisplayRating(int rating)
    {
        if (stars == null || stars.Length == 0)
        {
            Debug.LogError("WaveRatingStarsView nemá priradené hviezdy.", this);
            return;
        }

        // UI má päť hviezd. Rating 6 znamená Perfect, nie šiestu hviezdu.
        int visibleStars = Mathf.Clamp(rating, 0, stars.Length);

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null)
            {
                Debug.LogError($"Hviezda na indexe {i} nie je priradená.", this);
                continue;
            }

            stars[i].color = i < visibleStars
                ? activeColor
                : inactiveColor;
        }

        if (perfectText != null)
            perfectText.gameObject.SetActive(rating == 6);
    }
}