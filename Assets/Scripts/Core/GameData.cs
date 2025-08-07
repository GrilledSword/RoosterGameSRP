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
    public string sceneName;
    public int lastCheckpointIndex;

    // Alap�rtelmezett �rt�kek egy �j j�t�khoz
    public GameData()
    {
        playerPosition = Vector3.zero; // Ezt majd a kezd�poz�ci�ra kell �ll�tani
        currentHealth = 100;
        score = 0;
        inventoryItems = new List<ItemDataSerializable>();
        sceneName = "SampleScene"; // Cser�ld le a kezd�p�lya nev�re
        lastCheckpointIndex = -1; // -1 jelenti, hogy nincs checkpoint
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
