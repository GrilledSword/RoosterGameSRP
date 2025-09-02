using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon Item")]
public class WeaponItemDefinition : ItemDefinition
{
    [Header("Weapon Specifics")]
    public GameObject projectilePrefab;
    public float shootDuration; // Cooldown
    public float shootDistance; // Lõtáv

    [Header("Resource Costs")]
    public float manaCost = 0;
    public float staminaCost = 0;
}
