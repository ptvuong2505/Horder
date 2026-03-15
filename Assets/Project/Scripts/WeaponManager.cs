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
    // Dùng Awake để Setup trước khi các AutoGun.Start() chạy
    void Awake()
    {
        if (guns.Count == 0 || gunDataList.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] Chưa gán guns hoặc gunDataList trong Inspector!");
            return;
        }

        // Setup dữ liệu và bật TẤT CẢ súng cùng lúc
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] == null) continue;

            if (i < gunDataList.Count && gunDataList[i] != null)
            {
                guns[i].Setup(gunDataList[i]);
                guns[i].gameObject.SetActive(true);
                Debug.Log($"[WeaponManager] Bật súng [{i}]: {gunDataList[i].gunName}");
            }
            else
            {
                Debug.LogWarning($"[WeaponManager] Súng [{i}] không có GunData, bỏ qua.");
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Getter (giữ để tương thích)
    // ──────────────────────────────────────────────
    public int CurrentIndex => 0;
    public GunData CurrentGunData => (gunDataList.Count > 0) ? gunDataList[0] : null;
    public int GunCount => guns.Count;
}
