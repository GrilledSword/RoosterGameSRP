using UnityEngine;

/// <summary>
/// Defines the category of an item, which controls its behavior and placement in the inventory.
/// </summary>
public enum ItemCategory
{
    General, // Fogyasztható tárgyak, power-upok, kulcsok, stb.
    Weapon,  // Támadást lehetõvé tévõ tárgyak.
    Coin     // Azonnal pontot adó tárgyak, amik nem kerülnek az inventory-ba.
}

[CreateAssetMenu(fileName = "NewItemDefinition", menuName = "PekkaKana3/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic Item Properties")]
    public int itemID;
    public string itemName;
    [TextArea(3, 5)]
    public string itemDescription;
    public Sprite itemIcon;

    [Header("Categorization & Stacking")]
    [Tooltip("A tárgy kategóriája, ami meghatározza, hova kerül az inventory-ban (vagy hogy bekerül-e egyáltalán).")]
    public ItemCategory category = ItemCategory.General;
    [Tooltip("Stackelhetõ-e a tárgy az inventory-ban?")]
    public bool isStackable = true;
    [Tooltip("A maximális mennyiség, amennyi egy slotban elfér ebbõl a tárgyból.")]
    public int maxStackSize = 99;

    [Header("Gameplay Values")]
    [Tooltip("Mennyi pontot kap a játékos a tárgy felvételéért.")]
    public int scoreValue = 0;

    [Header("Item Sounds")]
    public AudioClip pickupSound;
    public AudioClip useSound;
}