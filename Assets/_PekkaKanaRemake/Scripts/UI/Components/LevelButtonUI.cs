using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButtonUI : MonoBehaviour
{
    [Header("Komponens Referenci�k")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private GameObject lockIcon;

    private LevelNodeDefinition levelData;
    private GameFlowManager gameFlowManager; // Referencia a menedzserre

    // JAV�TVA: A Setup met�dus most m�r megkapja a GameFlowManager referenci�j�t,
    // �gy a WorldMapManager nem ad hib�t.
    public void Setup(LevelNodeDefinition node, GameFlowManager flowManager)
    {
        this.levelData = node;
        this.gameFlowManager = flowManager;

        if (levelNameText != null)
        {
            levelNameText.text = node.levelName;
        }

        button.onClick.RemoveAllListeners(); // El�z� listener-ek t�rl�se
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
        // JAV�TVA: K�zvetlen parancsot k�ld�nk a szervernek a p�lya elind�t�s�ra.
        // Ezt b�rmelyik kliens megteheti, a szerver majd bet�lti mindenkinek.
        if (gameFlowManager != null && levelData != null)
        {
            // A StartLevelServerRpc met�dust haszn�ljuk, ami minden kliensnek bet�lti a p�ly�t.
            gameFlowManager.StartLevelServerRpc(levelData.levelSceneName);
        }
    }
}

