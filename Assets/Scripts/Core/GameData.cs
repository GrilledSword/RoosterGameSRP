using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // Játékos adatai
    public Vector3 playerPosition;
    public float currentHealth;
    public int score;

    // Inventory adatai
    public List<ItemDataSerializable> inventoryItems;

    // Pálya adatai
    public string sceneName;
    public int lastCheckpointIndex;

    // Alapértelmezett értékek egy új játékhoz
    public GameData()
    {
        playerPosition = Vector3.zero; // Ezt majd a kezdõpozícióra kell állítani
        currentHealth = 100;
        score = 0;
        inventoryItems = new List<ItemDataSerializable>();
        sceneName = "SampleScene"; // Cseréld le a kezdõpálya nevére
        lastCheckpointIndex = -1; // -1 jelenti, hogy nincs checkpoint
    }
}

// Az ItemData nem szerializálható alapból a FixedString miatt,
// ezért létrehozunk egy egyszerûsített, szerializálható verziót.
[System.Serializable]
public struct ItemDataSerializable
{
    public int itemID;
    public int quantity;
    public bool isEmpty;
}
