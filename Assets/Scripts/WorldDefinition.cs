using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Egy teljes vil�got (pl. "Rooster Island") defini�l� ScriptableObject.
/// Tartalmazza a vil�g nev�t, a t�rk�p jelenet�t �s az �sszes hozz� tartoz� p�ly�t.
/// </summary>
[CreateAssetMenu(fileName = "NewWorld", menuName = "PekkaKana3/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [Header("Vil�g Adatok")]
    [Tooltip("A vil�g azonos�t�ja, pl. 'world_01'. Ment�shez haszn�ljuk.")]
    public string worldId;

    [Tooltip("A vil�g megjelen�tend� neve, pl. 'Rooster Island'.")]
    public string worldName;

    [Tooltip("A vil�g t�rk�p�t tartalmaz� jelenet neve.")]
    public string worldMapSceneName;

    [Tooltip("A vil�g h�ttere a vil�gv�laszt� k�perny�n.")]
    public Sprite worldIcon;

    [Header("P�lya Lista")]
    [Tooltip("Az ebben a vil�gban tal�lhat� �sszes p�lya defin�ci�ja.")]
    public List<LevelNodeDefinition> levels;
}
