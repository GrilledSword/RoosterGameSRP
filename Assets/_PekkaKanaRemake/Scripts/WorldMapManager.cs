using UnityEngine;
using System.Collections.Generic;

public class WorldMapManager : MonoBehaviour
{
    [Header("UI Referenciák")]
    [Tooltip("A panel, ami a pálya-gombokat tartalmazza. A gombok ehhez igazodnak.")]
    [SerializeField] private RectTransform mapContainer;
    [Tooltip("A LevelButtonUI komponenst tartalmazó gomb prefabja.")]
    [SerializeField] private LevelButtonUI levelButtonPrefab;

    private WorldDefinition currentWorld;
    private GameFlowManager gameFlowManager;
    private Dictionary<string, LevelNodeDefinition> worldLevels;

    void Start()
    {
        gameFlowManager = GameFlowManager.Instance;
        currentWorld = gameFlowManager.GetCurrentWorldDefinition();
        worldLevels = new Dictionary<string, LevelNodeDefinition>();
        foreach (var level in currentWorld.levels)
        {
            worldLevels[level.levelId] = level;
        }

        GenerateMapUI();
    }

    void GenerateMapUI()
    {
        foreach (Transform child in mapContainer)
        {
            Destroy(child.gameObject);
        }

        HashSet<string> completedLevelIds = new HashSet<string>();
        foreach (var netId in gameFlowManager.CompletedLevelIds)
        {
            completedLevelIds.Add(netId.ToString());
        }

        for (int i = 0; i < currentWorld.levels.Count; i++)
        {
            LevelNodeDefinition node = currentWorld.levels[i];
            LevelButtonUI buttonInstance = Instantiate(levelButtonPrefab, mapContainer);
            buttonInstance.gameObject.SetActive(true);

            RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(node.positionX, node.positionY);
            buttonRect.anchorMax = new Vector2(node.positionX, node.positionY);
            buttonRect.anchoredPosition = Vector2.zero;

            buttonInstance.Setup(node, gameFlowManager);

            bool isUnlocked = CheckIfLevelIsUnlocked(node, completedLevelIds);
            if (i == 0)
            {
                isUnlocked = true;
            }

            buttonInstance.SetLockedState(!isUnlocked);
            buttonInstance.gameObject.SetActive(true);
        }
    }

    private bool CheckIfLevelIsUnlocked(LevelNodeDefinition nodeToCheck, HashSet<string> completedIds)
    {
        if (nodeToCheck.unlockedByDefault)
        {
            return true;
        }
        foreach (var level in currentWorld.levels)
        {
            if (level.unlocksNodes.Contains(nodeToCheck))
            {
                if (completedIds.Contains(level.levelId))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
