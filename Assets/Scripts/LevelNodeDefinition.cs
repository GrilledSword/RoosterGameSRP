using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Egyetlen pályacsomópontot definiál a világtérképen.
/// </summary>
[CreateAssetMenu(fileName = "NewLevelNode", menuName = "PekkaKana3/Level Node Definition")]
public class LevelNodeDefinition : ScriptableObject
{
    [Header("Pálya Adatok")]
    [Tooltip("A pálya egyedi azonosítója, pl. 'level_1_1'. Mentéshez használjuk.")]
    public string levelId;

    [Tooltip("A pálya megjelenítendõ neve, pl. 'Grassy Hills'.")]
    public string levelName;

    [Tooltip("A tényleges játékmenetet tartalmazó jelenet neve.")]
    public string levelSceneName;

    [Header("Térkép és Haladás")]
    [Tooltip("Alapból fel van-e oldva ez a pálya?")]
    public bool unlockedByDefault = false;

    [Tooltip("A pálya pozíciója a világtérkép UI-ján (normalizált koordinátákban, 0-1).")]
    [Range(0, 1)] public float positionX;
    [Range(0, 1)] public float positionY;

    [Header("Feloldások")]
    [Tooltip("Mely pályacsomópontok nyílnak meg ennek a pályának a teljesítése után.")]
    public List<LevelNodeDefinition> unlocksNodes;
}
