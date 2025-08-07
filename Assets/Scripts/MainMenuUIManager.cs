using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Menedzser Referenci�k")]
    [SerializeField] private ServerListManager serverListManager;
    [SerializeField] private GameFlowManager gameFlowManager;

    [Header("Panelek")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject worldSelectPanel; // �J

    [Header("Multiplayer UI")]
    [SerializeField] private TMP_InputField serverNameInputField;
    [SerializeField] private Toggle isPublicToggle;

    [Header("Vil�gv�laszt� UI")] // �J
    [SerializeField] private Transform worldSelectContent;
    [SerializeField] private GameObject worldButtonPrefab;

    void Start()
    {
        // Biztons�gi keres�s, ha nincsenek be�ll�tva
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

    // --- Gombkezel�k ---

    public void OnMultiplayerButtonClicked()
    {
        ShowPanel(multiplayerPanel);
        if (serverListManager != null) serverListManager.RefreshServerList();
    }

    public void OnHostButtonClicked()
    {
        // A Host gomb mostant�l a vil�gv�laszt� panelt nyitja meg.
        // A t�nyleges hostol�s a vil�g kiv�laszt�sa UT�N t�rt�nik.
        ShowPanel(worldSelectPanel);
        PopulateWorldSelectUI();
    }

    private void PopulateWorldSelectUI()
    {
        // T�r�lj�k a r�gi gombokat
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

            // Hozz�adjuk a listener-t, ami elind�tja a hostol�st �s a vil�got
            buttonGO.GetComponent<Button>().onClick.AddListener(() => {
                StartHostAndSelectWorld(world.worldId);
            });
        }
    }

    private void StartHostAndSelectWorld(string worldId)
    {
        if (serverListManager != null)
        {
            // �J: Beolvassuk a Toggle �llapot�t, miel�tt hostolunk.
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
        Debug.Log("Kil�p�s a j�t�kb�l...");
        Application.Quit();
    }
}
