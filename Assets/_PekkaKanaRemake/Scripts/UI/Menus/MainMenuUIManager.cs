using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    // JAVÍTVA: Singleton minta hozzáadva
    public static MainMenuUIManager Instance { get; private set; }

    [Header("Menedzser Referenciák")]
    [SerializeField] private ServerListManager serverListManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private LoadingPopUpManager loadingPopUpManager;

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

        if (serverListManager == null) serverListManager = FindFirstObjectByType<ServerListManager>();
        if (gameFlowManager == null) gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();
    }

    void Start()
    {
        ShowPanel(mainPanel);
        if (serverNameInputField != null) serverNameInputField.text = "Pekka's Game";
    }
    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene")
        {
            FindMainMenuSceneObjects();
        }
    }
    private void FindMainMenuSceneObjects()
    {
        ServerListManager serverListManager = GetComponent<ServerListManager>();
        GameObject mainMenuCanvasObject = GameObject.Find("MainMenuCanvas");

        if (mainMenuCanvasObject == null)
        {
            Debug.LogError("Nem található 'MainMenuCanvas' nevû objektum a jelenetben!");
            return;
        }
        Transform canvasTransform = mainMenuCanvasObject.transform;

        mainPanel = canvasTransform.Find("MainPanel")?.gameObject;
        multiplayerPanel = canvasTransform.Find("MultiplayerPanel")?.gameObject;
        settingsPanel = canvasTransform.Find("SettingsPanel")?.gameObject;
        worldSelectPanel = canvasTransform.Find("WorldSelectPanel")?.gameObject;
        loadGamePanel = canvasTransform.Find("LoadGamePanel")?.gameObject;
        serverNameInputField = canvasTransform.Find("MultiplayerPanel/ServerNameInputField")?.GetComponent<TMP_InputField>();
        isPublicToggle = canvasTransform.Find("MultiplayerPanel/PublicPrivateToggle")?.GetComponent<Toggle>();
        worldSelectContent = canvasTransform.Find("WorldSelectPanel/WorldSelectContent")?.GetComponent<Transform>();

        if (serverListManager == null)
        {
            return;
        }
        Transform canvasListTransform = serverListManager.transform;
        serverListManager.serverListContent = canvasTransform.Find("MultiplayerPanel/Scroll View/Viewport/ServerListContent")?.GetComponent<Transform>();

        SetupButtonListeners();

        if (mainPanel == null) Debug.LogError("A 'MainPanel' objektumot nem sikerült megtalálni! Ellenõrizd a nevét és a hierarchiát.");
        if (multiplayerPanel == null) Debug.LogError("A 'MultiplayerPanel' objektumot nem sikerült megtalálni! Ellenõrizd a nevét és a hierarchiát.");

        if (worldButtonPrefab == null)
        {
            string prefabPath = "Prefabs/MainMenu/WorldButton"; // A Resources mappán belüli relatív útvonal
            worldButtonPrefab = Resources.Load<GameObject>(prefabPath);

            if (worldButtonPrefab == null)
            {
                Debug.LogError($"A prefab betöltése sikertelen! Nincs '{worldButtonPrefabName}' nevû prefab a 'Resources/{prefabPath}' útvonalon.");
            }
        }
    }
    private void SetupButtonListeners()
    {
        if (mainPanel != null)
        {
            // FONTOS: A gombok nevének a Transform hierachiában pontosan meg kell egyeznie az itt megadottakkal!
            // Példa: MainPanel -> Gombok -> SinglePlayerButton

            Button singlePlayerBtn = FindDeepChild(mainPanel.transform, "SinglePlayerButton")?.GetComponent<Button>();
            if (singlePlayerBtn != null)
            {
                singlePlayerBtn.onClick.RemoveAllListeners(); // Biztonsági okokból töröljük a régieket
                singlePlayerBtn.onClick.AddListener(OnSingleplayerButtonClicked); // Hozzáadjuk a metódust
            }

            Button multiplayerBtn = FindDeepChild(mainPanel.transform, "MultiplayerButton")?.GetComponent<Button>();
            if (multiplayerBtn != null)
            {
                multiplayerBtn.onClick.RemoveAllListeners();
                multiplayerBtn.onClick.AddListener(OnMultiplayerButtonClicked);
            }

            Button settingsBtn = FindDeepChild(mainPanel.transform, "SettingsButton")?.GetComponent<Button>();
            if (settingsBtn != null)
            {
                settingsBtn.onClick.RemoveAllListeners();
                settingsBtn.onClick.AddListener(OnSettingsButtonClicked);
            }
            Button loadBtn = FindDeepChild(mainPanel.transform, "LoadButton")?.GetComponent<Button>();
            if (loadBtn != null)
            {
                loadBtn.onClick.RemoveAllListeners();
                loadBtn.onClick.AddListener(OnLoadGameButtonClicked);
            }

            Button exitBtn = FindDeepChild(mainPanel.transform, "ExitButton")?.GetComponent<Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick.RemoveAllListeners();
                exitBtn.onClick.AddListener(OnQuitButtonClicked);
            }
        }
        if (multiplayerPanel != null)
        {
            Button hostGameBtn = FindDeepChild(multiplayerPanel.transform, "HostGameButton")?.GetComponent<Button>();
            if (hostGameBtn != null)
            {
                hostGameBtn.onClick.RemoveAllListeners();
                hostGameBtn.onClick.AddListener(OnHostButtonClicked);
            }

            Button refreshListBtn = FindDeepChild(multiplayerPanel.transform, "RefreshListButton")?.GetComponent<Button>();
            if (refreshListBtn != null)
            {
                refreshListBtn.onClick.RemoveAllListeners();
                refreshListBtn.onClick.AddListener(OnRefreshButtonClicked);
            }

            Button backBtn = FindDeepChild(multiplayerPanel.transform, "BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(OnBackToMainButtonClicked);
            }
        }

        if (settingsPanel != null)
        {
            Button backBtn = FindDeepChild(settingsPanel.transform, "BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(OnBackToMainButtonClicked);
            }
        }

        if (loadGamePanel != null)
        {
            Button backBtn = FindDeepChild(loadGamePanel.transform, "BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(OnBackToMainButtonClicked);
            }
        }

        if (worldSelectPanel != null)
        {
            Button backBtn = FindDeepChild(worldSelectPanel.transform, "BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(OnBackToMainButtonClicked);
            }
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
    public void OnSingleplayerButtonClicked()
    {
        saveManager.ClearLoadedData();
        loadingPopUpManager.ShowLoadingPopUp();
        if (worldSelectPanel != null)
        {
            mainPanel.SetActive(false);
            worldSelectPanel.SetActive(true);
            PopulateWorldSelectUI();
        }
        else
        {
            Debug.Log("Single Player gomb megnyomva, játék indítása...");
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartSingleplayerGame("World1_MapScene");
            }
        }
    }

    public void OnMultiplayerButtonClicked()
    {
        saveManager.ClearLoadedData();
        loadingPopUpManager.ShowLoadingPopUp();
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
            GameData loadedData = saveManager.CurrentlyLoadedData;
            if (loadedData == null || string.IsNullOrEmpty(loadedData.lastSceneName))
            {
                Debug.LogError($"A(z) {slotIndex} mentési hely hibás vagy üres.");
                return;
            }

            saveManager.IsLoading = true;
            if (loadedData.isMultiplayer)
            {
                Debug.Log("Multiplayer mentés betöltése...");
                GameFlowManager.Instance.StartMultiplayerGameAsHost(loadedData.lastSceneName);
            }
            else
            {
                Debug.Log("Singleplayer mentés betöltése...");
                GameFlowManager.Instance.StartSingleplayerGame(loadedData.lastSceneName);
            }
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
