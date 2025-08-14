using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Egyetlen p�ly�t reprezent�l� gombot kezel a vil�gt�rk�pen.
/// </summary>
public class LevelButtonUI : MonoBehaviour
{
    [Header("Komponens Referenci�k")]
    [Tooltip("A gomb, amire a j�t�kos kattint.")]
    [SerializeField] private Button button;
    [Tooltip("A sz�veg, ami a p�lya nev�t �rja ki.")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [Tooltip("Egy GameObject (pl. egy lakat ikon), ami a z�rolt �llapotot jelzi.")]
    [SerializeField] private GameObject lockIcon;

    private LevelNodeDefinition levelData;
    private GameFlowManager gameFlowManager;

    /// <summary>
    /// A WorldMapManager h�vja meg, hogy be�ll�tsa a gombot a megfelel� adatokkal.
    /// </summary>
    public void Setup(LevelNodeDefinition nodeData, GameFlowManager manager)
    {
        this.levelData = nodeData;
        this.gameFlowManager = manager;

        if (levelNameText != null)
        {
            levelNameText.text = nodeData.levelName;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>
    /// Be�ll�tja a gomb �llapot�t (kattinthat� vagy z�rolt).
    /// </summary>
    public void SetLockedState(bool isLocked)
    {
        button.interactable = !isLocked;
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }
    }

    /// <summary>
    /// Lefut, amikor a j�t�kos a gombra kattint.
    /// </summary>
    private void OnButtonClicked()
    {
        // Csak a Host ind�that p�ly�t.
        if (Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"P�lya ind�t�sa: {levelData.levelSceneName}");
            gameFlowManager.StartLevelServerRpc(levelData.levelSceneName);
        }
    }
}
