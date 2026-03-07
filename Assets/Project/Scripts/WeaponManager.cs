using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WeaponManager – Gắn vào Player.
/// Quản lý 4 khẩu súng, tất cả cùng bắn enemy gần nhất đồng thời.
/// Mỗi AutoGun tự xoay và tự bắn độc lập.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Gun Slots (4 khẩu, gắn AutoGun prefab vào đây)")]
    public List<AutoGun> guns = new List<AutoGun>();  // Gán 4 child AutoGun object

    [Header("Gun Data (4 ScriptableObject GunData tương ứng)")]
    public List<GunData> gunDataList = new List<GunData>(); // Gán 4 GunData asset

    // Event thông báo (giữ để tương thích nếu UI dùng)
    public event Action<int, GunData> OnWeaponChanged;    // ──────────────────────────────────────────────
    // Awake: setup GunData, tắt hết, TỰ TẠO GunShop nếu chưa có
    void Awake()
    {
        if (guns.Count == 0 || gunDataList.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] Chưa gán guns hoặc gunDataList trong Inspector!");
            return;
        }

        // Tắt hết trước
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] == null) continue;
            if (i < gunDataList.Count && gunDataList[i] != null)
                guns[i].Setup(gunDataList[i]);
            guns[i].gameObject.SetActive(false);
        }

        // Tự tạo GunShop nếu chưa có trong scene
        if (GunShop.Instance == null)
        {
            GameObject shopGO = new GameObject("GunShop");
            GunShop shop = shopGO.AddComponent<GunShop>();
            shop.allGuns = new List<GunData>(gunDataList);
            shop.ReinitUnlockState();
            Debug.Log("[WeaponManager] Đã tự tạo GunShop.");
        }
    }

    void Start()
    {
        // Bật súng đã unlock
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] == null) continue;
            bool on = GunShop.Instance != null && GunShop.Instance.IsUnlocked(i);
            guns[i].gameObject.SetActive(on);
            if (on) Debug.Log($"[WeaponManager] Bật súng [{i}]: {gunDataList[i]?.gunName}");
        }

        // Wire GunShopUI (đã có trong scene) vào GameManager
        var shopUI = FindFirstObjectByType<GunShopUI>();
        if (shopUI != null && GameManager.Instance != null)
            GameManager.Instance.gunShopUI = shopUI;
        else if (shopUI == null)
            Debug.LogWarning("[WeaponManager] Không tìm thấy GunShopUI trong scene. Chạy Tools > Setup Gun Shop UI trước.");
    }

    // ──────────────────────────────────────────────
    //  Gọi từ GunShop khi player mở khóa súng mới
    // ──────────────────────────────────────────────
    public void ActivateGunSlot(int index)
    {
        if (index < 0 || index >= guns.Count) return;
        if (guns[index] == null) return;

        if (index < gunDataList.Count && gunDataList[index] != null)
            guns[index].Setup(gunDataList[index]);

        guns[index].gameObject.SetActive(true);
        Debug.Log($"[WeaponManager] Kích hoạt súng mới [{index}]: {gunDataList[index]?.gunName}");
        OnWeaponChanged?.Invoke(index, gunDataList[index]);
    }

    // ──────────────────────────────────────────────
    //  Getter (giữ để tương thích)
    // ──────────────────────────────────────────────
    public int CurrentIndex => 0;
    public GunData CurrentGunData => (gunDataList.Count > 0) ? gunDataList[0] : null;
    public int GunCount => guns.Count;
}
