using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Be�ll�t�sok")]
    [Tooltip("A j�t�kk�zbeni men� UI-t tartalmaz� prefab. Ezt h�zd be az Inspectorban.")]
    [SerializeField] private GameObject inGameMenuPrefab;
    [SerializeField] private InGameMenuUI _currentMenuInstance;
    public static bool GameIsPaused { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        if (scene.buildIndex >= 2)
        {
            _currentMenuInstance = FindAnyObjectByType<InGameMenuUI>();
            if (_currentMenuInstance == null && inGameMenuPrefab != null)
            {
                GameObject menuObject = Instantiate(inGameMenuPrefab);
                _currentMenuInstance = menuObject.GetComponent<InGameMenuUI>();
            }
            if (_currentMenuInstance == null)
            {
                Debug.LogError("InGameMenuManager: Nem siker�lt megtal�lni vagy l�trehozni az InGameMenuUI-t! Ellen�rizd, hogy a prefab be van-e h�zva az Inspectorban, �s hogy a prefabon van InGameMenuUI szkript.");
                return;
            }
        }
    }
    public void ToggleMenu()
    {
        if (_currentMenuInstance == null) return;
        _currentMenuInstance.Toggle();
        GameIsPaused = _currentMenuInstance.IsMenuOpen();
    }
}
