using Assets.Project.Scripts;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// PlayerSelectManager – Singleton qu?n lý vi?c m? khóa và ch?n Player.
/// G?n vào GameObject trong scene Menu/PlayerSelect.
/// Yêu c?u gán PlayerRegistry.
/// </summary>
public class PlayerSelectManager : MonoBehaviour
{
    [Header("Data")]
    public PlayerRegistry playerRegistry;

    private SaveData saveData;

    public PlayerData SelectedPlayer { get; private set; }

    public event Action OnSaveChanged;

    public static PlayerSelectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerSelectManager>();

                if (_instance == null)
                {
                    GameObject obj = new GameObject("PlayerSelectManager");
                    _instance = obj.AddComponent<PlayerSelectManager>();
                }
            }

            return _instance;
        }
    }

    private static PlayerSelectManager _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (playerRegistry == null)
            playerRegistry = ResolvePlayerRegistry();

        if (playerRegistry == null)
        {
            Debug.LogError("[PlayerSelectManager] Missing PlayerRegistry. Assign it in inspector, place it in a Resources folder, or keep it at Assets/Project/Data/Player/PlayerRegistry.asset.");
            saveData = SaveSystem.Load() ?? new SaveData();
            return;
        }

        LoadData();
    }

    void LoadData()
    {
        saveData = SaveSystem.Load() ?? new SaveData();

        if (saveData.unlockedPlayers == null)
            saveData.unlockedPlayers = new System.Collections.Generic.List<string>();

        bool dirty = false;

        foreach (var p in playerRegistry.allPlayers)
        {
            if (p.unlockedByDefault && !saveData.unlockedPlayers.Contains(p.playerID))
            {
                saveData.unlockedPlayers.Add(p.playerID);
                dirty = true;
            }
        }

        if (dirty)
            SaveSystem.Save(saveData);

        if (!string.IsNullOrEmpty(saveData.selectedPlayerId))
            SelectedPlayer = playerRegistry.GetByID(saveData.selectedPlayerId);

        if (SelectedPlayer == null)
            SelectedPlayer = GetPreferredOrDefaultPlayer();

        if (SelectedPlayer != null && saveData.selectedPlayerId != SelectedPlayer.playerID)
        {
            saveData.selectedPlayerId = SelectedPlayer.playerID;
            SaveSystem.Save(saveData);
        }
    }

    public PlayerData GetPreferredOrDefaultPlayer()
    {
        if (SelectedPlayer != null)
            return SelectedPlayer;

        if (playerRegistry == null || playerRegistry.allPlayers == null || playerRegistry.allPlayers.Count == 0)
            return null;

        //if (saveData != null && saveData.unlockedPlayers != null)
        //{
        //    foreach (var id in saveData.unlockedPlayers)
        //    {
        //        var unlocked = playerRegistry.GetByID(id);
        //        if (unlocked != null && unlocked.playerPrefab != null)
        //            return unlocked;
        //    }
        //}

        foreach (var p in playerRegistry.allPlayers)
        {
            if (p != null && p.unlockedByDefault && p.playerPrefab != null)
                return p;
        }

        //foreach (var p in playerRegistry.allPlayers)
        //{
        //    if (p != null && p.playerPrefab != null)
        //        return p;
        //}

        return null;
    }

    private PlayerRegistry ResolvePlayerRegistry()
    {
        var registry = Resources.Load<PlayerRegistry>("PlayerRegistry");
        if (registry != null)
            return registry;

        registry = Resources.LoadAll<PlayerRegistry>(string.Empty).FirstOrDefault();
        if (registry != null)
            return registry;

#if UNITY_EDITOR
        registry = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerRegistry>("Assets/Project/Data/Player/PlayerRegistry.asset");
        if (registry != null)
            return registry;

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlayerRegistry");
        if (guids != null && guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            registry = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerRegistry>(path);
            if (registry != null)
                return registry;
        }
#endif

        return null;
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
