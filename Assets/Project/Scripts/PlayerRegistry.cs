using Assets.Project.Scripts;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerRegistry – ScriptableObject ch?a toàn b? PlayerData trong game.
/// T?o asset: chu?t ph?i > Create > Game/Player Registry
/// Gán t?t c? PlayerData vào list "allPlayers".
/// </summary>
[CreateAssetMenu(fileName = "PlayerRegistry", menuName = "Game/Player Registry")]
public class PlayerRegistry : ScriptableObject
{
    public List<PlayerData> allPlayers = new List<PlayerData>();

    public PlayerData GetByID(string id)
    {
        return allPlayers.Find(p => p.playerID == id);
    }
}
