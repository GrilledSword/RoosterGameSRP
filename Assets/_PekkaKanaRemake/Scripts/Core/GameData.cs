using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public Vector3 playerPosition;
    public float currentHealth;
    public float currentMana;
    public float currentStamina;
    public int score;
    public List<ItemDataSerializable> inventoryItems;
    public List<string> completedLevelIds;
    public bool isMultiplayer;
    public string lastUpdated;
    public string lastSceneName;
    public List<string> collectedItemIds = new List<string>();
    public Vector3 cameraPosition;
}

[Serializable]
public class ItemDataSerializable
{
    public int itemID;
    public int quantity;
    public bool isEmpty;
}
