using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // J�t�kos adatai
    public Vector3 playerPosition;
    public float currentHealth;
    public int score;

    // Inventory adatai
    public List<ItemDataSerializable> inventoryItems;

    // P�lya adatai
    public string lastSceneName; // �tnevezve, hogy egy�rtelm�bb legyen
    public int lastCheckpointIndex;

    // --- �J R�SZ ---
    // Halad�si adatok
    // Egy lista, ami a teljes�tett p�ly�k 'levelId'-jait t�rolja.
    public List<string> completedLevelIds;
    // --- �J R�SZ V�GE ---

    // Alap�rtelmezett �rt�kek egy �j j�t�khoz
    public GameData()
    {
        playerPosition = Vector3.zero;
        currentHealth = 100;
        score = 0;
        inventoryItems = new List<ItemDataSerializable>();
        lastSceneName = "MainMenuScene"; // Kezd�dj�n a f�men�ben
        lastCheckpointIndex = -1;
        completedLevelIds = new List<string>(); // �res lista �j j�t�kn�l
    }
}

// Az ItemData nem szerializ�lhat� alapb�l a FixedString miatt,
// ez�rt l�trehozunk egy egyszer�s�tett, szerializ�lhat� verzi�t.
[System.Serializable]
public struct ItemDataSerializable
{
    public int itemID;
    public int quantity;
    public bool isEmpty;
}
