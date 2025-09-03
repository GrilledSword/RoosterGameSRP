using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Beállítások")]
    [Tooltip("A játékközbeni menü UI-t tartalmazó prefab. Ezt húzd be az Inspectorban.")]
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
