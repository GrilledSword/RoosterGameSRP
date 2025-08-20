using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Be�ll�t�sok")]
    [Tooltip("A j�t�kk�zbeni men� UI-t tartalmaz� prefab.")]
    [SerializeField] private GameObject inGameMenuPrefab;
    [Tooltip("A prefab neve a Resources mapp�ban, ha fentebb nincs hozz�rendelve.")]
    [SerializeField] private string inGameMenuPrefabName = "InGameMenuCanvas";

    private InGameMenuUI _currentMenuInstance;

    void Awake()
    {
        // JAV�TVA: Singleton minta
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // JAV�TVA: Automatikus prefab bet�lt�s
        if (inGameMenuPrefab == null)
        {
            inGameMenuPrefab = Resources.Load<GameObject>($"_PekkaKanaRemake/Prefabs/UI/{inGameMenuPrefabName}");
            if (inGameMenuPrefab == null)
            {
                Debug.LogError($"InGameMenuManager: '{inGameMenuPrefabName}' nev� prefab nem tal�lhat� a '_PekkaKanaRemake/Resources/Prefabs/UI/' mapp�ban!");
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
