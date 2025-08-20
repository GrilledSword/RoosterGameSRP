using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class InGameMenuUI : MonoBehaviour
{
    [Header("UI Panelek")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Gombok")]
    [SerializeField] private GameObject saveButton;

    [Header("Visszajelzés")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    private bool isMenuOpen = false;

    void Start()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }
    public void Toggle()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen && saveButton != null && NetworkManager.Singleton != null)
        {
            saveButton.SetActive(NetworkManager.Singleton.IsHost);
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            bool isSinglePlayer = NetworkManager.Singleton.ConnectedClientsList.Count == 1;
            if (isSinglePlayer)
            {
                Time.timeScale = isMenuOpen ? 0f : 1f;
            }
        }
    }

    public void OnContinueButtonClicked()
    {
        Toggle();
    }

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            SaveManager.Instance.SaveGame(0);
            ShowFeedback("Játék mentve!");
        }
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

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
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
