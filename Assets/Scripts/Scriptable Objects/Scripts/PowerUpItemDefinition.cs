using UnityEngine;

// Defines properties specific to power-up items (e.g., temporary buffs, special attacks).
// Inherits from ItemDefinition to get basic item properties.
[CreateAssetMenu(fileName = "NewPowerUpItem", menuName = "PekkaKana3/Items/PowerUp Item")]
public class PowerUpItemDefinition : ItemDefinition
{
    [Header("Power-Up Properties")]
    [Tooltip("Duration of the power-up effect.")]
    public float duration = 5f;

    [Tooltip("Multiplier for movement speed during power-up.")]
    public float speedMultiplier = 1f;

    [Tooltip("Additional damage dealt during power-up (e.g., for Super Egg).")]
    public float bonusDamage = 0;

    [Tooltip("Is the player temporarily invincible during this power-up?")]
    public bool grantsInvincibility = false;

    // You can add more specific properties for different power-up types (e.g., projectileType for Super Egg)
}
