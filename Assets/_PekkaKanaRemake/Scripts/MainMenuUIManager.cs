using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class MainMenuUIManager : MonoBehaviour
{
    // JAV�TVA: Singleton minta hozz�adva
    public static MainMenuUIManager Instance { get; private set; }

    [Header("Menedzser Referenci�k")]
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

    [Header("Vil�gv�laszt� UI")]
    [SerializeField] private Transform worldSelectContent;
    [SerializeField] private GameObject worldButtonPrefab;
    [SerializeField] private string worldButtonPrefabName = "WorldButton";

    void Awake()
    {
        // JAV�TVA: Singleton minta
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Mivel ez a men� csak a f�men�ben l�tezik, nem kell DontDestroyOnLoad

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

        // JAV�TVA: Panelek automatikus keres�se n�v alapj�n
        if (mainPanel == null) mainPanel = transform.Find("MainPanel")?.gameObject;
        if (multiplayerPanel == null) multiplayerPanel = transform.Find("MultiplayerPanel")?.gameObject;
        if (settingsPanel == null) settingsPanel = transform.Find("SettingsPanel")?.gameObject;
        if (worldSelectPanel == null) worldSelectPanel = transform.Find("WorldSelectPanel")?.gameObject;
        if (loadGamePanel == null) loadGamePanel = transform.Find("LoadGamePanel")?.gameObject;

        // JAV�TVA: Prefab automatikus bet�lt�se
        if (worldButtonPrefab == null)
        {
            worldButtonPrefab = Resources.Load<GameObject>($"_PekkaKanaRemake/Prefabs/UI/{worldButtonPrefabName}");
        }
    }

    // ... A t�bbi met�dus v�ltozatlan ...
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
            Debug.LogError($"A(z) {slotIndex} ment�si hely bet�lt�se sikertelen.");
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
        Debug.Log("Kil�p�s a j�t�kb�l...");
        Application.Quit();
    }
}
