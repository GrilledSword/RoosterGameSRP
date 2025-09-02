using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public Vector3 position;
    public int health;
    public int score;
    public int stamina;
    public int mana;

    public PlayerData()
    {
        playerName = "Pekka";
        position = Vector3.zero;
        health = 100;
        score = 0;
        stamina = 100;
        mana = 100;
    }
}