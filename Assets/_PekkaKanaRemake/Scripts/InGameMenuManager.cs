using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Beállítások")]
    [Tooltip("A játékközbeni menü UI-t tartalmazó prefab.")]
    [SerializeField] private GameObject inGameMenuPrefab;
    [Tooltip("A prefab neve a Resources mappában, ha fentebb nincs hozzárendelve.")]
    [SerializeField] private string inGameMenuPrefabName = "InGameMenuCanvas";

    private InGameMenuUI _currentMenuInstance;

    void Awake()
    {
        // JAVÍTVA: Singleton minta
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // JAVÍTVA: Automatikus prefab betöltés
        if (inGameMenuPrefab == null)
        {
            inGameMenuPrefab = Resources.Load<GameObject>($"_PekkaKanaRemake/Prefabs/UI/{inGameMenuPrefabName}");
            if (inGameMenuPrefab == null)
            {
                Debug.LogError($"InGameMenuManager: '{inGameMenuPrefabName}' nevû prefab nem található a '_PekkaKanaRemake/Resources/Prefabs/UI/' mappában!");
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_currentMenuInstance != null)
        {
            Destroy(_currentMenuInstance.gameObject);
            _currentMenuInstance = null;
        }

        if (scene.buildIndex >= 2 && inGameMenuPrefab != null)
        {
            GameObject menuObject = Instantiate(inGameMenuPrefab);
            _currentMenuInstance = menuObject.GetComponent<InGameMenuUI>();
        }
    }

    public void ToggleMenu()
    {
        if (_currentMenuInstance != null)
        {
            _currentMenuInstance.Toggle();
        }
    }
}
