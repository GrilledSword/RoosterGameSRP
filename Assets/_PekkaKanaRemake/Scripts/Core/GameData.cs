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
    public string lastSceneName; // Átnevezve, hogy egyértelmûbb legyen
    public int lastCheckpointIndex;

    // --- ÚJ RÉSZ ---
    // Haladási adatok
    // Egy lista, ami a teljesített pályák 'levelId'-jait tárolja.
    public List<string> completedLevelIds;
    // --- ÚJ RÉSZ VÉGE ---

    // Alapértelmezett értékek egy új játékhoz
    public GameData()
    {
        playerPosition = Vector3.zero;
        currentHealth = 100;
        score = 0;
        inventoryItems = new List<ItemDataSerializable>();
        lastSceneName = "MainMenuScene"; // Kezdõdjön a fõmenüben
        lastCheckpointIndex = -1;
        completedLevelIds = new List<string>(); // Üres lista új játéknál
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
