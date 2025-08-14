using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Egyetlen pályát reprezentáló gombot kezel a világtérképen.
/// </summary>
public class LevelButtonUI : MonoBehaviour
{
    [Header("Komponens Referenciák")]
    [Tooltip("A gomb, amire a játékos kattint.")]
    [SerializeField] private Button button;
    [Tooltip("A szöveg, ami a pálya nevét írja ki.")]
    [SerializeField] private TextMeshProUGUI levelNameText;
    [Tooltip("Egy GameObject (pl. egy lakat ikon), ami a zárolt állapotot jelzi.")]
    [SerializeField] private GameObject lockIcon;

    private LevelNodeDefinition levelData;
    private GameFlowManager gameFlowManager;

    /// <summary>
    /// A WorldMapManager hívja meg, hogy beállítsa a gombot a megfelelõ adatokkal.
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
    /// Beállítja a gomb állapotát (kattintható vagy zárolt).
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
    /// Lefut, amikor a játékos a gombra kattint.
    /// </summary>
    private void OnButtonClicked()
    {
        // Csak a Host indíthat pályát.
        if (Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"Pálya indítása: {levelData.levelSceneName}");
            gameFlowManager.StartLevelServerRpc(levelData.levelSceneName);
        }
    }
}
