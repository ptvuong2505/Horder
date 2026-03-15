using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WeaponManager – Gắn vào Player.
/// Quản lý 4 khẩu súng theo chế độ chỉ dùng 1 súng tại 1 thời điểm.
/// Đổi súng bằng phím 1/2/3/4 (chỉ đổi được súng đã mở khóa).
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Gun Slots (4 khẩu, gắn AutoGun prefab vào đây)")]
    public List<AutoGun> guns = new List<AutoGun>();  // Gán 4 child AutoGun object

    [Header("Gun Data (4 ScriptableObject GunData tương ứng)")]
    public List<GunData> gunDataList = new List<GunData>(); // Gán 4 GunData asset

    [Header("Switch Keys (New Input System)")]
    public Key keyGun1 = Key.Digit1;
    public Key keyGun2 = Key.Digit2;
    public Key keyGun3 = Key.Digit3;
    public Key keyGun4 = Key.Digit4;

    // Event thông báo (giữ để tương thích nếu UI dùng)
    public event Action<int, GunData> OnWeaponChanged;

    private int currentIndex = -1;

    // ──────────────────────────────────────────────
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
        // Chỉ bật đúng 1 súng đầu tiên đã unlock
        EquipFirstUnlocked();

        // Wire GunShopUI (đã có trong scene) vào GameManager
        var shopUI = FindFirstObjectByType<GunShopUI>();
        if (shopUI != null && GameManager.Instance != null)
            GameManager.Instance.gunShopUI = shopUI;
        else if (shopUI == null)
            Debug.LogWarning("[WeaponManager] Không tìm thấy GunShopUI trong scene. Chạy Tools > Setup Gun Shop UI trước.");
    }

    void Update()
    {
        if (Keyboard.current == null || GunShop.Instance == null) return;

        if (Keyboard.current[keyGun1].wasPressedThisFrame) TryEquipUnlocked(0);
        if (Keyboard.current[keyGun2].wasPressedThisFrame) TryEquipUnlocked(1);
        if (Keyboard.current[keyGun3].wasPressedThisFrame) TryEquipUnlocked(2);
        if (Keyboard.current[keyGun4].wasPressedThisFrame) TryEquipUnlocked(3);
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

        // Khi mở khóa súng mới, chuyển luôn sang súng vừa mua
        EquipOnly(index);
        Debug.Log($"[WeaponManager] Kích hoạt và trang bị súng [{index}]: {gunDataList[index]?.gunName}");
    }

    void EquipFirstUnlocked()
    {
        if (GunShop.Instance == null)
        {
            // Fallback nếu GunShop chưa sẵn sàng: bật slot 0
            EquipOnly(0);
            return;
        }

        for (int i = 0; i < guns.Count; i++)
        {
            if (GunShop.Instance.IsUnlocked(i))
            {
                EquipOnly(i);
                return;
            }
        }

        // Không có súng nào unlock thì tắt hết
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] != null) guns[i].gameObject.SetActive(false);
        }
        currentIndex = -1;
        Debug.LogWarning("[WeaponManager] Chưa có súng nào được mở khóa.");
    }

    void TryEquipUnlocked(int index)
    {
        if (index < 0 || index >= guns.Count) return;
        if (GunShop.Instance == null) return;
        if (!GunShop.Instance.IsUnlocked(index))
        {
            Debug.Log($"[WeaponManager] Súng [{index + 1}] chưa mở khóa.");
            return;
        }

        EquipOnly(index);
    }

    void EquipOnly(int index)
    {
        if (index < 0 || index >= guns.Count) return;

        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] == null) continue;
            bool active = (i == index);
            guns[i].gameObject.SetActive(active);
        }

        currentIndex = index;
        GunData data = (index >= 0 && index < gunDataList.Count) ? gunDataList[index] : null;
        OnWeaponChanged?.Invoke(index, data);
        Debug.Log($"[WeaponManager] Trang bị súng [{index + 1}]: {data?.gunName}");
    }

    // ──────────────────────────────────────────────
    //  Getter (giữ để tương thích)
    // ──────────────────────────────────────────────
    public int CurrentIndex => currentIndex;
    public GunData CurrentGunData =>
        (currentIndex >= 0 && currentIndex < gunDataList.Count) ? gunDataList[currentIndex] : null;
    public int GunCount => guns.Count;
}
