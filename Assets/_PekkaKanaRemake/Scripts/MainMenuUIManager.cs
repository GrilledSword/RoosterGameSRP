using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // Szükséges a ToArray()-hez
using Unity.Collections;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Menedzser Referenciák")]
    [SerializeField] private ServerListManager serverListManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private SaveManager saveManager; // ÚJ REFERENCIA

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
    [SerializeField] private GameObject worldButtonPrefab; // Ennek a prefabnak már a WorldButtonUI scripten kell lennie!
    [SerializeField] private string worldButtonPrefabName = "WorldButton";

    private void Awake()
    {
        FindMissingReferences();
    }

    void Start()
    {
        ShowPanel(mainPanel);
        if (serverNameInputField != null) serverNameInputField.text = "Pekka's Game";
    }

    public void FindMissingReferences()
    {
        if (serverListManager == null) serverListManager = FindFirstObjectByType<ServerListManager>();
        if (gameFlowManager == null) gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();

        // Panelek keresése név alapján, ha hiányoznak
        if (mainPanel == null) mainPanel = transform.Find("MainPanel")?.gameObject;
        if (multiplayerPanel == null) multiplayerPanel = transform.Find("MultiplayerPanel")?.gameObject;
        if (settingsPanel == null) settingsPanel = transform.Find("SettingsPanel")?.gameObject;
        if (worldSelectPanel == null) worldSelectPanel = transform.Find("WorldSelectPanel")?.gameObject;
        if (loadGamePanel == null) loadGamePanel = transform.Find("LoadGamePanel")?.gameObject;

        if (mainPanel == null) Debug.LogError("MainMenuUIManager: 'MainPanel' nem található a gyerekobjektumok között!");
        if (multiplayerPanel == null) Debug.LogError("MainMenuUIManager: 'MultiplayerPanel' nem található a gyerekobjektumok között!");
        if (settingsPanel == null) Debug.LogError("MainMenuUIManager: 'SettingsPanel' nem található a gyerekobjektumok között!");
        if (worldSelectPanel == null) Debug.LogError("MainMenuUIManager: 'WorldSelectPanel' nem található a gyerekobjektumok között!");
        if (loadGamePanel == null) Debug.LogError("MainMenuUIManager: 'LoadGamePanel' nem található a gyerekobjektumok között!");

        // Prefab betöltése Resources-ból
        if (worldButtonPrefab == null)
        {
            worldButtonPrefab = Resources.Load<GameObject>(worldButtonPrefabName);
            if (worldButtonPrefab == null)
            {
                Debug.LogError($"MainMenuUIManager: '{worldButtonPrefabName}' nevû prefab nem található az 'Assets/Resources' mappában!");
            }
        }
    }

    private void ShowPanel(GameObject panelToShow)
    {
        mainPanel.SetActive(panelToShow == mainPanel);
        multiplayerPanel.SetActive(panelToShow == multiplayerPanel);
        settingsPanel.SetActive(panelToShow == settingsPanel);
        worldSelectPanel.SetActive(panelToShow == worldSelectPanel);
        if (loadGamePanel != null) loadGamePanel.SetActive(panelToShow == loadGamePanel);
    }

    // --- Gombkezelõk ---

    public void OnMultiplayerButtonClicked()
    {
        // Töröljük a régi mentés adatokat, ha "új" multiplayer játékot indítunk.
        saveManager.ClearLoadedData();
        ShowPanel(multiplayerPanel);
        if (serverListManager != null) serverListManager.RefreshServerList();
    }

    public void OnHostButtonClicked()
    {
        // A Host gomb mostantól a világválasztó panelt nyitja meg.
        ShowPanel(worldSelectPanel);
        PopulateWorldSelectUI();
    }

    public void OnLoadGameButtonClicked()
    {
        ShowPanel(loadGamePanel);
    }

    // ÚJ METÓDUS: Ezt hívja meg a SaveSlotUI gombja.
    public void StartLoadFlow(int slotIndex)
    {
        if (saveManager.LoadGameData(slotIndex))
        {
            // Ha a betöltés sikeres, a multiplayer panelre ugrunk.
            ShowPanel(multiplayerPanel);
        }
        else
        {
            Debug.LogError($"A(z) {slotIndex} mentési hely betöltése sikertelen.");
        }
    }

    private void PopulateWorldSelectUI()
    {
        foreach (Transform child in worldSelectContent)
        {
            Destroy(child.gameObject);
        }

        List<WorldDefinition> worlds = gameFlowManager.GetAllWorlds();
        GameData loadedData = saveManager.CurrentlyLoadedData; // Lekérjük a betöltött adatokat.

        foreach (var world in worlds)
        {
            GameObject buttonGO = Instantiate(worldButtonPrefab, worldSelectContent);
            WorldButtonUI worldButton = buttonGO.GetComponent<WorldButtonUI>();

            // Átadjuk a gombnak a világot, a haladási adatokat és a kattintási eseményt.
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
        // Ha a multiplayer panelrõl lépünk vissza, leállítjuk a szerverkeresést.
        if (multiplayerPanel.activeSelf && serverListManager != null)
        {
            serverListManager.StopClientDiscovery();
        }

        // Ha a világválasztóról lépünk vissza (miután betöltöttünk egy mentést),
        // töröljük az ideiglenes adatokat, mert a játékos meggondolhatta magát.
        if (worldSelectPanel.activeSelf && saveManager != null)
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
