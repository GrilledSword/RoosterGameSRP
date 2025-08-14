using UnityEngine;
using System.Collections.Generic; // Sz�ks�ges a List �s a HashSet haszn�lat�hoz

/// <summary>
/// A vil�gt�rk�p logik�j�t vez�rli: l�trehozza �s menedzseli a p�lya-gombokat.
/// </summary>
public class WorldMapManager : MonoBehaviour
{
    [Header("UI Referenci�k")]
    [Tooltip("A panel, ami a p�lya-gombokat tartalmazza. A gombok ehhez igazodnak.")]
    [SerializeField] private RectTransform mapContainer;
    [Tooltip("A LevelButtonUI komponenst tartalmaz� gomb prefabja.")]
    [SerializeField] private LevelButtonUI levelButtonPrefab;

    private WorldDefinition currentWorld;
    private GameFlowManager gameFlowManager;
    private Dictionary<string, LevelNodeDefinition> worldLevels;

    void Start()
    {
        // Megkeress�k a GameFlowManager-t, ami a j�t�kos halad�s�t t�rolja.
        gameFlowManager = GameFlowManager.Instance;
        if (gameFlowManager == null)
        {
            Debug.LogError("WorldMapManager: GameFlowManager nem tal�lhat�! A jelenet nem fog m�k�dni.");
            return;
        }

        // Elk�rj�k az aktu�lis vil�g defin�ci�j�t.
        currentWorld = gameFlowManager.GetCurrentWorldDefinition();
        if (currentWorld == null)
        {
            Debug.LogError("WorldMapManager: Az aktu�lis vil�g nincs be�ll�tva a GameFlowManager-ben!");
            return;
        }

        // Fel�p�tj�k a p�ly�k t�rk�p�t a gyorsabb keres�shez
        worldLevels = new Dictionary<string, LevelNodeDefinition>();
        foreach (var level in currentWorld.levels)
        {
            worldLevels[level.levelId] = level;
        }

        GenerateMapUI();
    }

    /// <summary>
    /// Legener�lja a vil�gt�rk�p UI-j�t a WorldDefinition alapj�n.
    /// </summary>
    void GenerateMapUI()
    {
        // T�r�lj�k a r�gi gombokat, ha voltak.
        foreach (Transform child in mapContainer)
        {
            Destroy(child.gameObject);
        }

        // L�trehozunk egy list�t a m�r teljes�tett p�ly�kr�l a gyorsabb ellen�rz�shez.
        HashSet<string> completedLevelIds = new HashSet<string>();
        foreach (var netId in gameFlowManager.CompletedLevelIds)
        {
            completedLevelIds.Add(netId.ToString());
        }

        // V�gigmegy�nk a vil�g �sszes p�ly�j�n.
        foreach (LevelNodeDefinition node in currentWorld.levels)
        {
            // L�trehozzuk a gombot a prefab alapj�n.
            LevelButtonUI buttonInstance = Instantiate(levelButtonPrefab, mapContainer);

            // Be�ll�tjuk a gomb poz�ci�j�t a megadott normaliz�lt koordin�t�k alapj�n.
            RectTransform buttonRect = buttonInstance.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(node.positionX, node.positionY);
            buttonRect.anchorMax = new Vector2(node.positionX, node.positionY);
            buttonRect.anchoredPosition = Vector2.zero;

            // �tadjuk neki a sz�ks�ges adatokat.
            buttonInstance.Setup(node, gameFlowManager);

            // Eld�ntj�k, hogy a p�lya fel van-e oldva.
            bool isUnlocked = CheckIfLevelIsUnlocked(node, completedLevelIds);

            // Be�ll�tjuk a gomb �llapot�t.
            buttonInstance.SetLockedState(!isUnlocked);
        }
    }

    /// <summary>
    /// Seg�df�ggv�ny, ami ellen�rzi, hogy egy p�lya fel van-e oldva.
    /// </summary>
    private bool CheckIfLevelIsUnlocked(LevelNodeDefinition nodeToCheck, HashSet<string> completedIds)
    {
        // 1. Alapb�l fel van oldva?
        if (nodeToCheck.unlockedByDefault)
        {
            return true;
        }

        // 2. V�gigmegy�nk az �SSZES p�ly�n, �s megn�zz�k, van-e olyan, ami ezt a p�ly�t oldja fel.
        foreach (var level in currentWorld.levels)
        {
            // Ha ez a p�lya szerepel egy m�sik p�lya "felold�" list�j�ban...
            if (level.unlocksNodes.Contains(nodeToCheck))
            {
                // ...�s az a m�sik p�lya m�r teljes�tve van...
                if (completedIds.Contains(level.levelId))
                {
                    // ...akkor ez a p�lya is fel van oldva.
                    return true;
                }
            }
        }

        // Ha egyik felt�tel sem teljes�lt, akkor z�rolva van.
        return false;
    }
}
