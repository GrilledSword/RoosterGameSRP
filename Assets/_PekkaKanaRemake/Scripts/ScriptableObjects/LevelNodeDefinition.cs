using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevelNode", menuName = "PekkaKana3/Level Node Definition")]
public class LevelNodeDefinition : ScriptableObject
{
    [Header("P�lya Adatok")]
    [Tooltip("A p�lya egyedi azonos�t�ja, pl. 'level_1_1'. Ment�shez haszn�ljuk.")]
    public string levelId;

    [Tooltip("A p�lya megjelen�tend� neve, pl. 'Grassy Hills'.")]
    public string levelName;

    [Tooltip("A t�nyleges j�t�kmenetet tartalmaz� jelenet neve.")]
    public string levelSceneName;

    [Header("Time Limit")]
    [Tooltip("Does this level have a time limit?")]
    public bool hasTimeLimit = false;

    [Tooltip("The time limit for the level in seconds. Only effective if 'hasTimeLimit' is checked.")]
    public float timeLimitInSeconds = 120f;

    [Header("T�rk�p �s Halad�s")]
    [Tooltip("Alapb�l fel van-e oldva ez a p�lya?")]
    public bool unlockedByDefault = false;

    [Tooltip("A p�lya poz�ci�ja a vil�gt�rk�p UI-j�n (normaliz�lt koordin�t�kban, 0-1).")]
    [Range(0, 1)] public float positionX;
    [Range(0, 1)] public float positionY;

    [Header("Felold�sok")]
    [Tooltip("Mely p�lyacsom�pontok ny�lnak meg ennek a p�ly�nak a teljes�t�se ut�n.")]
    public List<LevelNodeDefinition> unlocksNodes;
}
