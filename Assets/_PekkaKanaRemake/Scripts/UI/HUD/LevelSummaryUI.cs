using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// Handles the end-of-level summary panel with animated score counting.
/// </summary>
public class LevelSummaryUI : MonoBehaviour
{
    [Header("UI Elemek")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI baseScoreText;
    [SerializeField] private TextMeshProUGUI timeBonusText;
    [SerializeField] private TextMeshProUGUI healthBonusText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private Button continueButton;

    [Header("Beállítások")]
    [SerializeField] private float countDuration = 1.5f;
    [SerializeField] private int pointsPerSecond = 10;
    [SerializeField] private int pointsPerHealth = 50;
    [SerializeField] private AudioClip countSound;
    //[SerializeField] private float countSoundTick = 1.0f;
    [SerializeField] private AudioClip finalScoreSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        summaryPanel.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClicked);
    }
    public void ShowSummary(int baseScore, float remainingTime, int remainingHealth)
    {
        if (summaryPanel.activeSelf) return;

        summaryPanel.SetActive(true);
        continueButton.gameObject.SetActive(false);
        StartCoroutine(AnimateSummary(baseScore, remainingTime, remainingHealth));
    }

    private IEnumerator AnimateSummary(int baseScore, float remainingTime, int remainingHealth)
    {
        baseScoreText.text = "0";
        timeBonusText.text = "0";
        healthBonusText.text = "0";
        totalScoreText.text = "0";

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(CountUpText(baseScoreText, baseScore));

        int timeBonus = Mathf.FloorToInt(remainingTime * pointsPerSecond);
        yield return StartCoroutine(CountUpText(timeBonusText, timeBonus));

        int healthBonus = remainingHealth * pointsPerHealth;
        yield return StartCoroutine(CountUpText(healthBonusText, healthBonus));

        int totalScore = baseScore + timeBonus + healthBonus;
        yield return StartCoroutine(CountUpText(totalScoreText, totalScore, true));

        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PekkaPlayerController>();
            if (playerController != null)
            {
                playerController.AddScoreServerRpc(timeBonus + healthBonus);
            }
        }

        continueButton.gameObject.SetActive(true);
    }

    private IEnumerator CountUpText(TextMeshProUGUI textElement, int targetValue, bool isFinal = false)
    {
        if (targetValue == 0)
        {
            textElement.text = "0";
            yield break;
        }

        if (audioSource != null && countSound != null)
        {
            audioSource.clip = countSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        float timer = 0;
        int startValue = 0;

        while (timer < countDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / countDuration;
            int currentValue = (int)Mathf.Lerp(startValue, targetValue, progress);
            textElement.text = currentValue.ToString();
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        textElement.text = targetValue.ToString();

        if (isFinal && finalScoreSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(finalScoreSound);
        }
    }

    private void OnContinueClicked()
    {
        if (GameFlowManager.Instance != null && NetworkManager.Singleton.IsServer)
        {
            GameFlowManager.Instance.ReturnToWorldMap();
        }
    }
}
