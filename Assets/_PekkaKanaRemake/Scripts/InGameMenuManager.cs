using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Be�ll�t�sok")]
    [Tooltip("A j�t�kk�zbeni men� UI-t tartalmaz� prefab. Ezt h�zd be az Inspectorban.")]
    [SerializeField] private GameObject inGameMenuPrefab;

    // Ez a v�ltoz� fogja t�rolni a jelenetben l�v�, t�nyleges men� objektumot (a Clone-t).
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
        // Ha m�r l�tezik men� p�ld�ny, biztons�gb�l megsemmis�tj�k.
        if (_currentMenuInstance != null)
        {
            Destroy(_currentMenuInstance.gameObject);
            _currentMenuInstance = null;
        }

        // Csak a j�t�kjelenetekben (pl. build index 2-t�l) keress�k vagy hozzuk l�tre a men�t.
        if (scene.buildIndex >= 2)
        {
            // El�sz�r megpr�b�ljuk megkeresni, h�tha m�r van a jelenetben egy InGameMenuUI.
            _currentMenuInstance = FindAnyObjectByType<InGameMenuUI>();

            // Ha nem tal�ltunk, �s a prefab be van �ll�tva az Inspectorban, akkor l�trehozzuk.
            if (_currentMenuInstance == null && inGameMenuPrefab != null)
            {
                GameObject menuObject = Instantiate(inGameMenuPrefab);
                _currentMenuInstance = menuObject.GetComponent<InGameMenuUI>();
            }

            // Ha m�g mindig nincs men�, akkor valami nagy baj van.
            if (_currentMenuInstance == null)
            {
                Debug.LogError("InGameMenuManager: Nem siker�lt megtal�lni vagy l�trehozni az InGameMenuUI-t! Ellen�rizd, hogy a prefab be van-e h�zva az Inspectorban, �s hogy a prefabon van InGameMenuUI szkript.");
                return;
            }
        }
    }

    /// <summary>
    /// Megjelen�ti vagy elrejti az aktu�lis men� p�ld�nyt.
    /// A PlayerController h�vja meg ezt a met�dust.
    /// </summary>
    public void ToggleMenu()
    {
        if (_currentMenuInstance != null)
        {
            _currentMenuInstance.Toggle();
        }
    }
}
