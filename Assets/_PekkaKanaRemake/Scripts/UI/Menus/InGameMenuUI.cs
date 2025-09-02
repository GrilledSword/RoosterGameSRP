using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class InGameMenuUI : MonoBehaviour
{
    [Header("UI Panelek")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject savePanel;

    [Header("Gombok")]
    [SerializeField] private GameObject saveButton;

    [Header("Visszajelzés")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    private bool isMenuOpen = false;

    void Start()
    {
        if (menuPanel) menuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (savePanel) savePanel.SetActive(false);
        if (feedbackText) feedbackText.gameObject.SetActive(false);
    }

    public bool IsMenuOpen() => isMenuOpen;
    public void Toggle()
    {

        if (savePanel != null && savePanel.activeSelf)
        {
            OnBackFromSaveButtonClicked();
            return;
        }
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            OnBackFromSettingsButtonClicked();
            return;
        }

        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen && saveButton != null && NetworkManager.Singleton != null)
        {
            saveButton.SetActive(NetworkManager.Singleton.IsHost);
        }

    }

    public void OnContinueButtonClicked() => Toggle();

    public void OnSettingsButtonClicked()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnBackFromSettingsButtonClicked()
    {
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OnSaveButtonClicked()
    {
        menuPanel.SetActive(false);
        savePanel.SetActive(true);
    }

    public void OnBackFromSaveButtonClicked()
    {
        savePanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OnExitToMainMenuButtonClicked()
    {
        Time.timeScale = 1f;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideFeedbackAfterDelay());
        }
    }

    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSecondsRealtime(feedbackDuration);
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }
}
