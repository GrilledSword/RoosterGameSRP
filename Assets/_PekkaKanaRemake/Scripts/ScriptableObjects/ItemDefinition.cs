using UnityEngine;

/// <summary>
/// Defines the category of an item, which controls its behavior and placement in the inventory.
/// </summary>
public enum ItemCategory
{
    General, // Fogyaszthat� t�rgyak, power-upok, kulcsok, stb.
    Weapon,  // T�mad�st lehet�v� t�v� t�rgyak.
    Coin     // Azonnal pontot ad� t�rgyak, amik nem ker�lnek az inventory-ba.
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
    [Tooltip("A t�rgy kateg�ri�ja, ami meghat�rozza, hova ker�l az inventory-ban (vagy hogy beker�l-e egy�ltal�n).")]
    public ItemCategory category = ItemCategory.General;
    [Tooltip("Stackelhet�-e a t�rgy az inventory-ban?")]
    public bool isStackable = true;
    [Tooltip("A maxim�lis mennyis�g, amennyi egy slotban elf�r ebb�l a t�rgyb�l.")]
    public int maxStackSize = 99;

    [Header("Gameplay Values")]
    [Tooltip("Mennyi pontot kap a j�t�kos a t�rgy felv�tel��rt.")]
    public int scoreValue = 0;

    [Header("Item Sounds")]
    public AudioClip pickupSound;
    public AudioClip useSound;
}