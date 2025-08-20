using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Beállítások")]
    [Tooltip("A játékközbeni menü UI-t tartalmazó prefab. Ezt húzd be az Inspectorban.")]
    [SerializeField] private GameObject inGameMenuPrefab;

    // Ez a változó fogja tárolni a jelenetben lévõ, tényleges menü objektumot (a Clone-t).
    [SerializeField] private InGameMenuUI _currentMenuInstance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        // Ha már létezik menü példány, biztonságból megsemmisítjük.
        if (_currentMenuInstance != null)
        {
            Destroy(_currentMenuInstance.gameObject);
            _currentMenuInstance = null;
        }

        // Csak a játékjelenetekben (pl. build index 2-tõl) keressük vagy hozzuk létre a menüt.
        if (scene.buildIndex >= 2)
        {
            // Elõször megpróbáljuk megkeresni, hátha már van a jelenetben egy InGameMenuUI.
            _currentMenuInstance = FindAnyObjectByType<InGameMenuUI>();

            // Ha nem találtunk, és a prefab be van állítva az Inspectorban, akkor létrehozzuk.
            if (_currentMenuInstance == null && inGameMenuPrefab != null)
            {
                GameObject menuObject = Instantiate(inGameMenuPrefab);
                _currentMenuInstance = menuObject.GetComponent<InGameMenuUI>();
            }

            // Ha még mindig nincs menü, akkor valami nagy baj van.
            if (_currentMenuInstance == null)
            {
                Debug.LogError("InGameMenuManager: Nem sikerült megtalálni vagy létrehozni az InGameMenuUI-t! Ellenõrizd, hogy a prefab be van-e húzva az Inspectorban, és hogy a prefabon van InGameMenuUI szkript.");
                return;
            }
        }
    }

    /// <summary>
    /// Megjeleníti vagy elrejti az aktuális menü példányt.
    /// A PlayerController hívja meg ezt a metódust.
    /// </summary>
    public void ToggleMenu()
    {
        if (_currentMenuInstance != null)
        {
            _currentMenuInstance.Toggle();
        }
    }
}
