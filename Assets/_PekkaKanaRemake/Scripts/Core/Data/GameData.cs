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
    public List<ItemDataSerializable> inventoryItems = new List<ItemDataSerializable>();

    public List<string> completedLevelIds = new List<string>();
    public bool isMultiplayer;
    public string saveName;
    public string lastUpdated;
    public string lastSceneName;
    public List<string> collectedItemIds = new List<string>();
    public Dictionary<string, PlayerData> playersData = new Dictionary<string, PlayerData>();
}

[Serializable]
public class ItemDataSerializable
{
    public int itemID;
    public int quantity;
    public bool isEmpty;
}
