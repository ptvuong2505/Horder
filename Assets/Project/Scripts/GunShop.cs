using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GunShop – Singleton quản lý trạng thái mở khóa từng khẩu súng.
/// Gắn vào cùng GameObject với WeaponManager hoặc GameManager.
/// </summary>
public class GunShop : MonoBehaviour
{
    public static GunShop Instance;

    [Header("Danh sách súng trong kho (giống thứ tự WeaponManager.gunDataList)")]
    public List<GunData> allGuns = new List<GunData>();

    // Trạng thái mở khóa từng slot
    private bool[] unlocked;

    // ── Events ──────────────────────────────────────────────────────────────
    /// <summary>Khi 1 khẩu súng được mua thành công (truyền index).</summary>
    public event Action<int> OnGunUnlocked;

    /// <summary>Khi shop đóng lại.</summary>
    public event Action OnShopClosed;

    // ────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitUnlockState();
    }

    void InitUnlockState()
    {
        unlocked = new bool[allGuns.Count];
        for (int i = 0; i < allGuns.Count; i++)
        {
            unlocked[i] = allGuns[i] != null &&
                          (allGuns[i].unlockedByDefault || allGuns[i].unlockCost == 0);
        }
    }

    /// <summary>Gọi sau khi assign allGuns bằng code (dùng bởi GunShopBootstrapper).</summary>
    public void ReinitUnlockState() => InitUnlockState();

    // ────────────────────────────────────────────────────────────────────────
    //  Query
    // ────────────────────────────────────────────────────────────────────────
    public bool IsUnlocked(int index)
    {
        if (unlocked == null || index < 0 || index >= unlocked.Length) return false;
        return unlocked[index];
    }

    public int GunCount => allGuns.Count;

    public GunData GetGunData(int index)
    {
        if (index < 0 || index >= allGuns.Count) return null;
        return allGuns[index];
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Mua súng
    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Trả về true nếu mua thành công.
    /// </summary>
    public bool TryUnlock(int index)
    {
        if (index < 0 || index >= allGuns.Count)
        {
            Debug.LogWarning($"[GunShop] Index {index} không hợp lệ.");
            return false;
        }

        if (unlocked[index])
        {
            Debug.Log($"[GunShop] Súng [{index}] đã được mở khóa rồi.");
            return false;
        }

        GunData gun = allGuns[index];
        if (gun == null) return false;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GunShop] Không tìm thấy GameManager!");
            return false;
        }

        if (GameManager.Instance.Coins < gun.unlockCost)
        {
            Debug.Log($"[GunShop] Không đủ coin. Cần {gun.unlockCost}, có {GameManager.Instance.Coins}.");
            return false;
        }

        // Trừ coin
        GameManager.Instance.SpendCoins(gun.unlockCost);

        // Đánh dấu mở khóa
        unlocked[index] = true;
        Debug.Log($"[GunShop] Mở khóa thành công: {gun.gunName}");

        // Báo WeaponManager bật súng này
        WeaponManager wm = FindFirstObjectByType<WeaponManager>();
        if (wm != null)
            wm.ActivateGunSlot(index);

        // Phát sự kiện
        OnGunUnlocked?.Invoke(index);
        return true;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Shop lifecycle
    // ────────────────────────────────────────────────────────────────────────
    public void CloseShop()
    {
        OnShopClosed?.Invoke();
    }
}
