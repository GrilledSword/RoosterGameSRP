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
    private GameFlowManager gameFlowManager;

    public void Setup(LevelNodeDefinition node, GameFlowManager flowManager)
    {
        this.levelData = node;
        this.gameFlowManager = flowManager;
        this.levelNameText.text = levelData.levelName;

        if (levelNameText != null)
        {
            levelNameText.text = node.levelName;
        }

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
        if (Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            gameFlowManager.StartLevelServerRpc(levelData.levelSceneName);
            GameFlowManager.Instance.SetSelectedLevel(levelData);
        }
    }
}