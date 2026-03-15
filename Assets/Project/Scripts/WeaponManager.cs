using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WeaponManager (gắn lên Player)
/// Quản lý các khẩu AutoGun con (gun slots) và gán GunData tương ứng.
/// 
/// Setup chuẩn:
/// - Player
///   - GunSlot_0 (AutoGun)
///   - GunSlot_1 (AutoGun)
///   - GunSlot_2 (AutoGun)
///   - GunSlot_3 (AutoGun)
/// 
/// Vì sao dùng Awake?
/// - Để gọi AutoGun.Setup(GunData) trước khi AutoGun.Start()/Update chạy,
///   tránh trường hợp gunData = null ở frame đầu.
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
