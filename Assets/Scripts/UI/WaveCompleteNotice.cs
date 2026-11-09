using System.Collections;
using ResortEmpire;
using TMPro;
using UnityEngine;

public class WaveCompleteNotice : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0.1f)] private float displaySeconds = 2.5f;

    private Coroutine hideCoroutine;

    private void OnEnable()
    {
        if (runner == null)
        {
            Debug.LogError("WaveCompleteNotice nemá priradený SimulationRunner.", this);
            return;
        }

        runner.WaveCompleted += ShowNotice;

        if (noticePanel != null)
            noticePanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.WaveCompleted -= ShowNotice;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    private void ShowNotice(int waveId)
    {
        if (messageText != null)
            messageText.SetText($"Wave {waveId} complete");

        if (noticePanel != null)
            noticePanel.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displaySeconds);

        if (noticePanel != null)
            noticePanel.SetActive(false);

        hideCoroutine = null;
    }
}