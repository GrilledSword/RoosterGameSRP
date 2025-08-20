using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class MainMenuUIManager : MonoBehaviour
{
    // JAVÍTVA: Singleton minta hozzáadva
    public static MainMenuUIManager Instance { get; private set; }

    [Header("Menedzser Referenciák")]
    [SerializeField] private ServerListManager serverListManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private SaveManager saveManager;

    [Header("Panelek")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject worldSelectPanel;
    [SerializeField] private GameObject loadGamePanel;

    [Header("Multiplayer UI")]
    [SerializeField] private TMP_InputField serverNameInputField;
    [SerializeField] private Toggle isPublicToggle;

    [Header("Világválasztó UI")]
    [SerializeField] private Transform worldSelectContent;
    [SerializeField] private GameObject worldButtonPrefab;
    [SerializeField] private string worldButtonPrefabName = "WorldButton";

    void Awake()
    {
        // JAVÍTVA: Singleton minta
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Mivel ez a menü csak a fõmenüben létezik, nem kell DontDestroyOnLoad

        FindMissingReferences();
    }

    void Start()
    {
        ShowPanel(mainPanel);
        if (serverNameInputField != null) serverNameInputField.text = "Pekka's Game";
    }

    private void FindMissingReferences()
    {
        if (serverListManager == null) serverListManager = FindFirstObjectByType<ServerListManager>();
        if (gameFlowManager == null) gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();

        // JAVÍTVA: Panelek automatikus keresése név alapján
        if (mainPanel == null) mainPanel = transform.Find("MainPanel")?.gameObject;
        if (multiplayerPanel == null) multiplayerPanel = transform.Find("MultiplayerPanel")?.gameObject;
        if (settingsPanel == null) settingsPanel = transform.Find("SettingsPanel")?.gameObject;
        if (worldSelectPanel == null) worldSelectPanel = transform.Find("WorldSelectPanel")?.gameObject;
        if (loadGamePanel == null) loadGamePanel = transform.Find("LoadGamePanel")?.gameObject;

        // JAVÍTVA: Prefab automatikus betöltése
        if (worldButtonPrefab == null)
        {
            worldButtonPrefab = Resources.Load<GameObject>($"_PekkaKanaRemake/Prefabs/UI/{worldButtonPrefabName}");
        }
    }

    // ... A többi metódus változatlan ...
    private void ShowPanel(GameObject panelToShow)
    {
        if (mainPanel) mainPanel.SetActive(panelToShow == mainPanel);
        if (multiplayerPanel) multiplayerPanel.SetActive(panelToShow == multiplayerPanel);
        if (settingsPanel) settingsPanel.SetActive(panelToShow == settingsPanel);
        if (worldSelectPanel) worldSelectPanel.SetActive(panelToShow == worldSelectPanel);
        if (loadGamePanel != null) loadGamePanel.SetActive(panelToShow == loadGamePanel);
    }

    public void OnMultiplayerButtonClicked()
    {
        saveManager.ClearLoadedData();
        ShowPanel(multiplayerPanel);
        if (serverListManager != null) serverListManager.RefreshServerList();
    }

    public void OnHostButtonClicked()
    {
        ShowPanel(worldSelectPanel);
        PopulateWorldSelectUI();
    }

    public void OnLoadGameButtonClicked()
    {
        ShowPanel(loadGamePanel);
    }

    public void StartLoadFlow(int slotIndex)
    {
        if (saveManager.LoadGameData(slotIndex))
        {
            ShowPanel(multiplayerPanel);
        }
        else
        {
            Debug.LogError($"A(z) {slotIndex} mentési hely betöltése sikertelen.");
        }
    }

    private void PopulateWorldSelectUI()
    {
        if (worldSelectContent == null) return;
        foreach (Transform child in worldSelectContent)
        {
            Destroy(child.gameObject);
        }

        List<WorldDefinition> worlds = gameFlowManager.GetAllWorlds();
        GameData loadedData = saveManager.CurrentlyLoadedData;

        foreach (var world in worlds)
        {
            GameObject buttonGO = Instantiate(worldButtonPrefab, worldSelectContent);
            WorldButtonUI worldButton = buttonGO.GetComponent<WorldButtonUI>();

            worldButton.Setup(world, loadedData, () => {
                StartHostAndSelectWorld(world.worldId);
            });
        }
    }

    private void StartHostAndSelectWorld(string worldId)
    {
        if (serverListManager != null)
        {
            serverListManager.HostAsPublic = isPublicToggle.isOn;
            serverListManager.ServerNameToHost = string.IsNullOrWhiteSpace(serverNameInputField.text) ? "Pekka Szerver" : serverNameInputField.text;
            serverListManager.StartHostOnly();
        }

        if (gameFlowManager != null)
        {
            if (saveManager.CurrentlyLoadedData != null)
            {
                string[] completedIdsStrings = saveManager.CurrentlyLoadedData.completedLevelIds.ToArray();
                FixedString32Bytes[] completedIdsFixed = new FixedString32Bytes[completedIdsStrings.Length];
                for (int i = 0; i < completedIdsStrings.Length; i++)
                {
                    completedIdsFixed[i] = new FixedString32Bytes(completedIdsStrings[i]);
                }

                gameFlowManager.ApplyLoadedProgressServerRpc(completedIdsFixed);

                saveManager.ClearLoadedData();
            }

            gameFlowManager.SelectWorldServerRpc(worldId);
        }

        if (worldSelectPanel) worldSelectPanel.SetActive(false);
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
        if (multiplayerPanel != null && multiplayerPanel.activeSelf && serverListManager != null)
        {
            serverListManager.StopClientDiscovery();
        }

        if (worldSelectPanel != null && worldSelectPanel.activeSelf && saveManager != null)
        {
            saveManager.ClearLoadedData();
        }

        ShowPanel(mainPanel);
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Kilépés a játékból...");
        Application.Quit();
    }
}
