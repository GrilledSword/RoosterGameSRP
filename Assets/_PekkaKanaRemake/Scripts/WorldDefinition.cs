using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Egy teljes világot (pl. "Rooster Island") definiáló ScriptableObject.
/// Tartalmazza a világ nevét, a térkép jelenetét és az összes hozzá tartozó pályát.
/// </summary>
[CreateAssetMenu(fileName = "NewWorld", menuName = "PekkaKana3/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [Header("Világ Adatok")]
    [Tooltip("A világ azonosítója, pl. 'world_01'. Mentéshez használjuk.")]
    public string worldId;

    [Tooltip("A világ megjelenítendõ neve, pl. 'Rooster Island'.")]
    public string worldName;

    [Tooltip("A világ térképét tartalmazó jelenet neve.")]
    public string worldMapSceneName;

    [Tooltip("A világ háttere a világválasztó képernyõn.")]
    public Sprite worldIcon;

    [Header("Pálya Lista")]
    [Tooltip("Az ebben a világban található összes pálya definíciója.")]
    public List<LevelNodeDefinition> levels;
}
