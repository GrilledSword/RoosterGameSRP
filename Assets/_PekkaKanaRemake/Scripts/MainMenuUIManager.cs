using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // Sz�ks�ges a ToArray()-hez
using Unity.Collections;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Menedzser Referenci�k")]
    [SerializeField] private ServerListManager serverListManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private SaveManager saveManager; // �J REFERENCIA

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
    [SerializeField] private GameObject worldButtonPrefab; // Ennek a prefabnak m�r a WorldButtonUI scripten kell lennie!
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

        // Panelek keres�se n�v alapj�n, ha hi�nyoznak
        if (mainPanel == null) mainPanel = transform.Find("MainPanel")?.gameObject;
        if (multiplayerPanel == null) multiplayerPanel = transform.Find("MultiplayerPanel")?.gameObject;
        if (settingsPanel == null) settingsPanel = transform.Find("SettingsPanel")?.gameObject;
        if (worldSelectPanel == null) worldSelectPanel = transform.Find("WorldSelectPanel")?.gameObject;
        if (loadGamePanel == null) loadGamePanel = transform.Find("LoadGamePanel")?.gameObject;

        if (mainPanel == null) Debug.LogError("MainMenuUIManager: 'MainPanel' nem tal�lhat� a gyerekobjektumok k�z�tt!");
        if (multiplayerPanel == null) Debug.LogError("MainMenuUIManager: 'MultiplayerPanel' nem tal�lhat� a gyerekobjektumok k�z�tt!");
        if (settingsPanel == null) Debug.LogError("MainMenuUIManager: 'SettingsPanel' nem tal�lhat� a gyerekobjektumok k�z�tt!");
        if (worldSelectPanel == null) Debug.LogError("MainMenuUIManager: 'WorldSelectPanel' nem tal�lhat� a gyerekobjektumok k�z�tt!");
        if (loadGamePanel == null) Debug.LogError("MainMenuUIManager: 'LoadGamePanel' nem tal�lhat� a gyerekobjektumok k�z�tt!");

        // Prefab bet�lt�se Resources-b�l
        if (worldButtonPrefab == null)
        {
            worldButtonPrefab = Resources.Load<GameObject>(worldButtonPrefabName);
            if (worldButtonPrefab == null)
            {
                Debug.LogError($"MainMenuUIManager: '{worldButtonPrefabName}' nev� prefab nem tal�lhat� az 'Assets/Resources' mapp�ban!");
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

    // --- Gombkezel�k ---

    public void OnMultiplayerButtonClicked()
    {
        // T�r�lj�k a r�gi ment�s adatokat, ha "�j" multiplayer j�t�kot ind�tunk.
        saveManager.ClearLoadedData();
        ShowPanel(multiplayerPanel);
        if (serverListManager != null) serverListManager.RefreshServerList();
    }

    public void OnHostButtonClicked()
    {
        // A Host gomb mostant�l a vil�gv�laszt� panelt nyitja meg.
        ShowPanel(worldSelectPanel);
        PopulateWorldSelectUI();
    }

    public void OnLoadGameButtonClicked()
    {
        ShowPanel(loadGamePanel);
    }

    // �J MET�DUS: Ezt h�vja meg a SaveSlotUI gombja.
    public void StartLoadFlow(int slotIndex)
    {
        if (saveManager.LoadGameData(slotIndex))
        {
            // Ha a bet�lt�s sikeres, a multiplayer panelre ugrunk.
            ShowPanel(multiplayerPanel);
        }
        else
        {
            Debug.LogError($"A(z) {slotIndex} ment�si hely bet�lt�se sikertelen.");
        }
    }

    private void PopulateWorldSelectUI()
    {
        foreach (Transform child in worldSelectContent)
        {
            Destroy(child.gameObject);
        }

        List<WorldDefinition> worlds = gameFlowManager.GetAllWorlds();
        GameData loadedData = saveManager.CurrentlyLoadedData; // Lek�rj�k a bet�lt�tt adatokat.

        foreach (var world in worlds)
        {
            GameObject buttonGO = Instantiate(worldButtonPrefab, worldSelectContent);
            WorldButtonUI worldButton = buttonGO.GetComponent<WorldButtonUI>();

            // �tadjuk a gombnak a vil�got, a halad�si adatokat �s a kattint�si esem�nyt.
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
        // Ha a multiplayer panelr�l l�p�nk vissza, le�ll�tjuk a szerverkeres�st.
        if (multiplayerPanel.activeSelf && serverListManager != null)
        {
            serverListManager.StopClientDiscovery();
        }

        // Ha a vil�gv�laszt�r�l l�p�nk vissza (miut�n bet�lt�tt�nk egy ment�st),
        // t�r�lj�k az ideiglenes adatokat, mert a j�t�kos meggondolhatta mag�t.
        if (worldSelectPanel.activeSelf && saveManager != null)
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
