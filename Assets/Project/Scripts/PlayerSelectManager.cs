using Assets.Project.Scripts;
using System;
using UnityEngine;

/// <summary>
/// PlayerSelectManager – Singleton qu?n lý vi?c m? khóa và ch?n Player.
/// G?n vào GameObject trong scene Menu/PlayerSelect.
/// Yêu c?u gán PlayerRegistry.
/// </summary>
public class PlayerSelectManager : MonoBehaviour
{
    public static PlayerSelectManager Instance;

    [Header("Data")]
    public PlayerRegistry playerRegistry;

    private SaveData saveData;

    public PlayerData SelectedPlayer { get; private set; }

    public event Action OnSaveChanged;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        saveData = SaveSystem.Load();

        // Mở khóa mặc định nhung player có unlockedByDefault
        bool dirty = false;
        foreach (var p in playerRegistry.allPlayers)
        {
            if (p.unlockedByDefault && !saveData.unlockedPlayers.Contains(p.playerID))
            {
                saveData.unlockedPlayers.Add(p.playerID);
                dirty = true;
            }
        }
        if (dirty) SaveSystem.Save(saveData);

        // Khôi ph?c player ?ã ch?n
        if (!string.IsNullOrEmpty(saveData.selectedPlayerId))
            SelectedPlayer = playerRegistry.GetByID(saveData.selectedPlayerId);

        // Fallback: ch?n player ??u tiên ?ã m? khóa
        if (SelectedPlayer == null && saveData.unlockedPlayers.Count > 0)
            SelectedPlayer = playerRegistry.GetByID(saveData.unlockedPlayers[0]);
    }

    public int GetGold() => saveData.gold;

    public bool IsUnlocked(PlayerData data)
        => saveData.unlockedPlayers.Contains(data.playerID);

    public bool IsSelected(PlayerData data)
        => SelectedPlayer != null && SelectedPlayer.playerID == data.playerID;

    /// <summary>Tr? v? true n?u m? khóa thành công.</summary>
    public bool TryUnlock(PlayerData data)
    {
        if (IsUnlocked(data)) return true;
        if (saveData.gold < data.unlockCost) return false;

        saveData.gold -= data.unlockCost;
        saveData.unlockedPlayers.Add(data.playerID);
        SaveSystem.Save(saveData);
        OnSaveChanged?.Invoke();
        return true;
    }

    public void SelectPlayer(PlayerData data)
    {
        if (!IsUnlocked(data)) return;

        SelectedPlayer = data;
        saveData.selectedPlayerId = data.playerID;
        SaveSystem.Save(saveData);
        OnSaveChanged?.Invoke();
    }

    /// <summary>Thêm vàng (g?i t? GameManager sau m?i màn).</summary>
    public void AddGold(int amount)
    {
        saveData.gold += amount;
        SaveSystem.Save(saveData);
        OnSaveChanged?.Invoke();
    }
}
