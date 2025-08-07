using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Menedzser Referenciák")]
    [SerializeField] private ServerListManager serverListManager;
    [SerializeField] private GameFlowManager gameFlowManager;

    [Header("Panelek")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject worldSelectPanel; // ÚJ

    [Header("Multiplayer UI")]
    [SerializeField] private TMP_InputField serverNameInputField;
    [SerializeField] private Toggle isPublicToggle;

    [Header("Világválasztó UI")] // ÚJ
    [SerializeField] private Transform worldSelectContent;
    [SerializeField] private GameObject worldButtonPrefab;

    void Start()
    {
        // Biztonsági keresés, ha nincsenek beállítva
        if (serverListManager == null) serverListManager = FindFirstObjectByType<ServerListManager>();
        if (gameFlowManager == null) gameFlowManager = FindFirstObjectByType<GameFlowManager>();

        ShowPanel(mainPanel);
        if (serverNameInputField != null) serverNameInputField.text = "Pekka's Game";
    }

    private void ShowPanel(GameObject panelToShow)
    {
        mainPanel.SetActive(panelToShow == mainPanel);
        multiplayerPanel.SetActive(panelToShow == multiplayerPanel);
        settingsPanel.SetActive(panelToShow == settingsPanel);
        worldSelectPanel.SetActive(panelToShow == worldSelectPanel);
    }

    // --- Gombkezelõk ---

    public void OnMultiplayerButtonClicked()
    {
        ShowPanel(multiplayerPanel);
        if (serverListManager != null) serverListManager.RefreshServerList();
    }

    public void OnHostButtonClicked()
    {
        // A Host gomb mostantól a világválasztó panelt nyitja meg.
        // A tényleges hostolás a világ kiválasztása UTÁN történik.
        ShowPanel(worldSelectPanel);
        PopulateWorldSelectUI();
    }

    private void PopulateWorldSelectUI()
    {
        // Töröljük a régi gombokat
        foreach (Transform child in worldSelectContent)
        {
            Destroy(child.gameObject);
        }

        List<WorldDefinition> worlds = gameFlowManager.GetAllWorlds();
        foreach (var world in worlds)
        {
            GameObject buttonGO = Instantiate(worldButtonPrefab, worldSelectContent);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = world.worldName;
            buttonGO.GetComponent<Image>().sprite = world.worldIcon;

            // Hozzáadjuk a listener-t, ami elindítja a hostolást és a világot
            buttonGO.GetComponent<Button>().onClick.AddListener(() => {
                StartHostAndSelectWorld(world.worldId);
            });
        }
    }

    private void StartHostAndSelectWorld(string worldId)
    {
        if (serverListManager != null)
        {
            // ÚJ: Beolvassuk a Toggle állapotát, mielõtt hostolunk.
            serverListManager.HostAsPublic = isPublicToggle.isOn;
            serverListManager.ServerNameToHost = string.IsNullOrWhiteSpace(serverNameInputField.text) ? "Pekka Szerver" : serverNameInputField.text;
            serverListManager.StartHostOnly();
        }

        if (gameFlowManager != null)
        {
            gameFlowManager.SelectWorldServerRpc(worldId);
        }

        worldSelectPanel.SetActive(false);
    }

    public void OnRefreshButtonClicked()
    {
        if (serverListManager != null) serverListManager.RefreshServerList();
    }

    public void OnSettingsButtonClicked()
    {
        ShowPanel(settingsPanel);
    }

    public void OnBackToMainButtonClicked()
    {
        if (multiplayerPanel.activeSelf && serverListManager != null)
        {
            serverListManager.StopClientDiscovery();
        }
        ShowPanel(mainPanel);
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Kilépés a játékból...");
        Application.Quit();
    }
}
