using UnityEngine;

// Defines properties specific to consumable items (e.g., food, potions).
// Inherits from ItemDefinition to get basic item properties.
[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "PekkaKana3/Items/Consumable Item")]
public class ConsumableItemDefinition : ItemDefinition
{
    [Header("Consumable Properties")]
    [Tooltip("Amount of health restored when consumed.")]
    public float healthRestored = 0;

    [Tooltip("Amount of energy/mana restored when consumed.")]
    public float energyRestored = 0;

    [Tooltip("Any temporary status effect applied (e.g., speed boost duration).")]
    public float effectDuration = 0; // Duration of any temporary effect
    // You could add an enum for effectType if you have different kinds of effects
} 