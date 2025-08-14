using UnityEngine;
using System.Collections.Generic; // Szükséges a List és a HashSet használatához

/// <summary>
/// A világtérkép logikáját vezérli: létrehozza és menedzseli a pálya-gombokat.
/// </summary>
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
        // Megkeressük a GameFlowManager-t, ami a játékos haladását tárolja.
        gameFlowManager = GameFlowManager.Instance;
        if (gameFlowManager == null)
        {
            Debug.LogError("WorldMapManager: GameFlowManager nem található! A jelenet nem fog mûködni.");
            return;
        }

        // Elkérjük az aktuális világ definícióját.
        currentWorld = gameFlowManager.GetCurrentWorldDefinition();
        if (currentWorld == null)
        {
            Debug.LogError("WorldMapManager: Az aktuális világ nincs beállítva a GameFlowManager-ben!");
            return;
        }

        // Felépítjük a pályák térképét a gyorsabb kereséshez
        worldLevels = new Dictionary<string, LevelNodeDefinition>();
        foreach (var level in currentWorld.levels)
        {
            worldLevels[level.levelId] = level;
        }

        GenerateMapUI();
    }

    /// <summary>
    /// Legenerálja a világtérkép UI-ját a WorldDefinition alapján.
    /// </summary>
    void GenerateMapUI()
    {
        // Töröljük a régi gombokat, ha voltak.
        foreach (Transform child in mapContainer)
        {
            Destroy(child.gameObject);
        }

        // Létrehozunk egy listát a már teljesített pályákról a gyorsabb ellenõrzéshez.
        HashSet<string> completedLevelIds = new HashSet<string>();
        foreach (var netId in gameFlowManager.CompletedLevelIds)
        {
            completedLevelIds.Add(netId.ToString());
        }

        // Végigmegyünk a világ összes pályáján.
        foreach (LevelNodeDefinition node in currentWorld.levels)
        {
            // Létrehozzuk a gombot a prefab alapján.
            LevelButtonUI buttonInstance = Instantiate(levelButtonPrefab, mapContainer);

            // Beállítjuk a gomb pozícióját a megadott normalizált koordináták alapján.
            RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(node.positionX, node.positionY);
            buttonRect.anchorMax = new Vector2(node.positionX, node.positionY);
            buttonRect.anchoredPosition = Vector2.zero;

            // Átadjuk neki a szükséges adatokat.
            buttonInstance.Setup(node, gameFlowManager);

            // Eldöntjük, hogy a pálya fel van-e oldva.
            bool isUnlocked = CheckIfLevelIsUnlocked(node, completedLevelIds);

            // Beállítjuk a gomb állapotát.
            buttonInstance.SetLockedState(!isUnlocked);
        }
    }

    /// <summary>
    /// Segédfüggvény, ami ellenõrzi, hogy egy pálya fel van-e oldva.
    /// </summary>
    private bool CheckIfLevelIsUnlocked(LevelNodeDefinition nodeToCheck, HashSet<string> completedIds)
    {
        // 1. Alapból fel van oldva?
        if (nodeToCheck.unlockedByDefault)
        {
            return true;
        }

        // 2. Végigmegyünk az ÖSSZES pályán, és megnézzük, van-e olyan, ami ezt a pályát oldja fel.
        foreach (var level in currentWorld.levels)
        {
            // Ha ez a pálya szerepel egy másik pálya "feloldó" listájában...
            if (level.unlocksNodes.Contains(nodeToCheck))
            {
                // ...és az a másik pálya már teljesítve van...
                if (completedIds.Contains(level.levelId))
                {
                    // ...akkor ez a pálya is fel van oldva.
                    return true;
                }
            }
        }

        // Ha egyik feltétel sem teljesült, akkor zárolva van.
        return false;
    }
}
