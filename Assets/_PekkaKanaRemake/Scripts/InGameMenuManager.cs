using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    [Header("Beállítások")]
    [Tooltip("A játékközbeni menü UI-t tartalmazó prefab. Az InGameMenuUI szkriptnek rajta kell lennie!")]
    [SerializeField] private InGameMenuUI inGameMenuPrefab;
    private InGameMenuUI _currentMenuInstance;

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
        }
        if (scene.buildIndex >= 2 && inGameMenuPrefab != null)
        {
            _currentMenuInstance = Instantiate(inGameMenuPrefab);
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
