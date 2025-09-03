using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButtonUI : MonoBehaviour
{
    [Header("Komponens Referenciák")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private GameObject lockIcon;

    private LevelNodeDefinition levelData;
    private GameFlowManager gameFlowManager; // Referencia a menedzserre

    // JAVÍTVA: A Setup metódus most már megkapja a GameFlowManager referenciáját,
    // így a WorldMapManager nem ad hibát.
    public void Setup(LevelNodeDefinition node, GameFlowManager flowManager)
    {
        this.levelData = node;
        this.gameFlowManager = flowManager;

        if (levelNameText != null)
        {
            levelNameText.text = node.levelName;
        }

        button.onClick.RemoveAllListeners(); // Elõzõ listener-ek törlése
        button.onClick.AddListener(OnButtonClicked);
    }

    public void SetLockedState(bool isLocked)
    {
        if (button != null)
        {
            button.interactable = !isLocked;
        }
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }
    }

    private void OnButtonClicked()
    {
        // JAVÍTVA: Közvetlen parancsot küldünk a szervernek a pálya elindítására.
        // Ezt bármelyik kliens megteheti, a szerver majd betölti mindenkinek.
        if (gameFlowManager != null && levelData != null)
        {
            // A StartLevelServerRpc metódust használjuk, ami minden kliensnek betölti a pályát.
            gameFlowManager.StartLevelServerRpc(levelData.levelSceneName);
        }
    }
}

