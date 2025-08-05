using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemManager", menuName = "PekkaKana3/Item Manager")]
public class ItemManager : ScriptableObject
{
    [Tooltip("List of all ItemDefinition ScriptableObjects in your game.")]
    public List<ItemDefinition> allItemDefinitions;
    private Dictionary<int, ItemDefinition> itemDictionary;
    public void Initialize()
    {
        if (itemDictionary == null)
        {
            itemDictionary = new Dictionary<int, ItemDefinition>();
            foreach (ItemDefinition itemDef in allItemDefinitions)
            {
                if (itemDef != null)
                {
                    if (itemDictionary.ContainsKey(itemDef.itemID))
                    {
                        Debug.LogWarning($"ItemManager: Duplicate Item ID found! ID: {itemDef.itemID}, Name: {itemDef.itemName}. Please ensure all item IDs are unique.");
                    }
                    else
                    {
                        itemDictionary.Add(itemDef.itemID, itemDef);
                    }
                }
            }
        }
    }
    public ItemDefinition GetItemDefinition(int id)
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
            if (itemDictionary == null || itemDictionary.Count == 0)
            {
                return null;
            }
        }

        if (itemDictionary.TryGetValue(id, out ItemDefinition itemDef))
        {
            return itemDef;
        }
        return null;
    }
    private static ItemManager _instance;
    public static ItemManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemManager>("ItemManager");
                if (_instance == null)
                {
                    Debug.LogError("ItemManager: Instance not found! Create an ItemManager ScriptableObject asset and place it in a Resources folder.");
                }
                else
                {
                    _instance.Initialize();
                }
            }
            return _instance;
        }
    }
}