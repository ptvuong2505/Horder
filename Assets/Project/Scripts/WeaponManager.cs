using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// WeaponManager – Gắn vào Player.
/// Quản lý nhiều khẩu súng theo chế độ 4 góc 4 súng.
/// Đổi súng nhanh bằng phím 1-4 (nếu khẩu tương ứng đã mở khóa).
/// </summary>
public class WeaponManager : MonoBehaviour
{
    const string EquippedGunKey = "EquippedGun";
    const int MaxFormationGuns = 4;
    const int TargetShopGunCount = 8;
    static readonly string[] GunGroups = { "Pistol", "Rifle", "Shotgun", "Sniper" };
    private List<Sprite> cachedBulletSprites;

    [Header("Gun Slots (gắn AutoGun prefab vào đây)")]
    public List<AutoGun> guns = new List<AutoGun>();

    [Header("Gun Data (ScriptableObject GunData tương ứng)")]
    public List<GunData> gunDataList = new List<GunData>();

    [Header("Switch Keys (New Input System)")]
    public Key keyGun1 = Key.Digit1;
    public Key keyGun2 = Key.Digit2;
    public Key keyGun3 = Key.Digit3;
    public Key keyGun4 = Key.Digit4;
    public Key keyGun5 = Key.Digit5;
    public Key keyGun6 = Key.Digit6;
    public Key keyGun7 = Key.Digit7;
    public Key keyGun8 = Key.Digit8;

    [Header("4-Gun Formation")]
    [Tooltip("Bật để luôn hiển thị tối đa 4 súng ở 4 góc quanh nhân vật.")]
    public bool useFourCornerFormation = true;
    [Tooltip("Khoảng cách ngang từ tâm nhân vật đến súng.")]
    public float cornerDistanceX = 1.8f;
    [Tooltip("Khoảng cách dọc từ tâm nhân vật đến súng.")]
    public float cornerDistanceY = 1.2f;

    // Event thông báo (giữ để tương thích nếu UI dùng)
    public event Action<int, GunData> OnWeaponChanged;

    private int currentIndex = -1;
    private readonly int[] formationSlotGunIndices = { 0, 1, 2, 3 };

    // ──────────────────────────────────────────────
    // Awake: setup GunData, tắt hết, TỰ TẠO GunShop nếu chưa có
    void Awake()
    {
        if (guns.Count == 0)
        {
            // Fallback: tự quét AutoGun con của Player nếu quên kéo thả ở Inspector.
            AutoGun[] foundGuns = GetComponentsInChildren<AutoGun>(true);
            for (int i = 0; i < foundGuns.Length; i++)
            {
                if (foundGuns[i] != null)
                    guns.Add(foundGuns[i]);
            }

            if (guns.Count > 0)
                Debug.Log($"[WeaponManager] Tự tìm thấy {guns.Count} AutoGun từ hierarchy.");
        }

        if (guns.Count == 0 || gunDataList.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] Chưa gán guns hoặc gunDataList trong Inspector!");
            return;
        }

        TryPopulateGunDataFromSprites();
        EnsureGunSlotsForAllData();

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
        EnsureFormationSlotDefaults();

        // Trang bị theo súng đầu tiên đã unlock
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

        if (Keyboard.current[keyGun1].wasPressedThisFrame) TryAssignGunToFormationSlot(0, 0);
        if (Keyboard.current[keyGun2].wasPressedThisFrame) TryAssignGunToFormationSlot(1, 1);
        if (Keyboard.current[keyGun3].wasPressedThisFrame) TryAssignGunToFormationSlot(2, 2);
        if (Keyboard.current[keyGun4].wasPressedThisFrame) TryAssignGunToFormationSlot(3, 3);
        if (Keyboard.current[keyGun5].wasPressedThisFrame) TryAssignGunToFormationSlot(4, 0);
        if (Keyboard.current[keyGun6].wasPressedThisFrame) TryAssignGunToFormationSlot(5, 1);
        if (Keyboard.current[keyGun7].wasPressedThisFrame) TryAssignGunToFormationSlot(6, 2);
        if (Keyboard.current[keyGun8].wasPressedThisFrame) TryAssignGunToFormationSlot(7, 3);

        if (useFourCornerFormation)
            ApplyGunFormation(GetActiveFormationIndices());
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

        int savedIndex = PlayerPrefs.GetInt(EquippedGunKey, -1);
        if (savedIndex >= 0 && savedIndex < guns.Count && GunShop.Instance.IsUnlocked(savedIndex))
        {
            EquipOnly(savedIndex);
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

        // Không có súng nào unlock: fallback trang bị slot 0 để tránh vào game không thấy súng.
        if (guns.Count > 0 && guns[0] != null)
        {
            Debug.LogWarning("[WeaponManager] Không có súng unlock, fallback trang bị slot 0.");
            EquipOnly(0);
            return;
        }

        for (int i = 0; i < guns.Count; i++)
            if (guns[i] != null) guns[i].gameObject.SetActive(false);

        currentIndex = -1;
        PlayerPrefs.DeleteKey(EquippedGunKey);
        PlayerPrefs.Save();
        Debug.LogWarning("[WeaponManager] Chưa có súng nào được mở khóa.");
    }

    void TryAssignGunToFormationSlot(int gunIndex, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= formationSlotGunIndices.Length) return;
        if (gunIndex < 0 || gunIndex >= guns.Count) return;
        if (GunShop.Instance == null) return;

        if (!GunShop.Instance.IsUnlocked(gunIndex))
        {
            Debug.Log($"[WeaponManager] Súng [{gunIndex + 1}] chưa mở khóa.");
            return;
        }

        formationSlotGunIndices[slotIndex] = gunIndex;
        EquipOnly(gunIndex);

        Debug.Log($"[WeaponManager] Slot {slotIndex + 1}: chuyển sang súng [{gunIndex + 1}].");
    }

    void EquipOnly(int index)
    {
        if (index < 0 || index >= guns.Count) return;

        List<int> activeFormationIndices = GetActiveFormationIndices();

        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] == null) continue;

            bool active = useFourCornerFormation
                ? activeFormationIndices.Contains(i)
                : (i == index);
            guns[i].gameObject.SetActive(active);

            if (active && i < gunDataList.Count && gunDataList[i] != null)
                guns[i].Setup(gunDataList[i]);
        }

        if (useFourCornerFormation)
            ApplyGunFormation(activeFormationIndices);

        currentIndex = index;
        PlayerPrefs.SetInt(EquippedGunKey, index);
        PlayerPrefs.Save();
        GunData data = (index >= 0 && index < gunDataList.Count) ? gunDataList[index] : null;
        OnWeaponChanged?.Invoke(index, data);
        Debug.Log($"[WeaponManager] Trang bị súng [{index + 1}]: {data?.gunName}");
    }

    List<int> GetActiveFormationIndices()
    {
        List<int> active = new List<int>(MaxFormationGuns);

        if (!useFourCornerFormation)
        {
            if (currentIndex >= 0 && currentIndex < guns.Count)
                active.Add(currentIndex);
            return active;
        }

        EnsureFormationSlotDefaults();

        for (int slot = 0; slot < MaxFormationGuns; slot++)
        {
            int idx = formationSlotGunIndices[slot];
            if (!IsUnlockedAndValid(idx))
            {
                idx = FindFallbackGun(slot, active);
                formationSlotGunIndices[slot] = idx;
            }

            if (idx >= 0)
                active.Add(idx);
        }

        if (active.Count == 0 && guns.Count > 0)
            active.Add(0);

        return active;
    }

    void ApplyGunFormation(List<int> activeIndices)
    {
        if (!useFourCornerFormation || activeIndices == null) return;

        Vector3[] corners = new Vector3[]
        {
            new Vector3(-cornerDistanceX,  cornerDistanceY, 0f),
            new Vector3( cornerDistanceX,  cornerDistanceY, 0f),
            new Vector3(-cornerDistanceX, -cornerDistanceY, 0f),
            new Vector3( cornerDistanceX, -cornerDistanceY, 0f)
        };

        int count = Mathf.Min(activeIndices.Count, corners.Length);
        for (int i = 0; i < count; i++)
        {
            int gunIndex = activeIndices[i];
            if (gunIndex < 0 || gunIndex >= guns.Count) continue;
            if (guns[gunIndex] == null) continue;
            guns[gunIndex].transform.localPosition = corners[i];
        }
    }

    void EnsureGunSlotsForAllData()
    {
        if (gunDataList == null) return;
        if (guns == null) guns = new List<AutoGun>();

        int required = gunDataList.Count;
        if (required <= guns.Count) return;

        AutoGun template = null;
        for (int i = 0; i < guns.Count; i++)
        {
            if (guns[i] != null)
            {
                template = guns[i];
                break;
            }
        }

        for (int i = guns.Count; i < required; i++)
        {
            AutoGun newSlot = null;

            if (template != null)
            {
                GameObject clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = $"AutoGun_{i + 1}";
                newSlot = clone.GetComponent<AutoGun>();
            }
            else
            {
                GameObject go = new GameObject($"AutoGun_{i + 1}");
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
                newSlot = go.AddComponent<AutoGun>();
            }

            if (newSlot != null)
            {
                newSlot.gameObject.SetActive(false);
                guns.Add(newSlot);
            }
        }

        Debug.Log($"[WeaponManager] Auto-created gun slots: {guns.Count}/{required}.");
    }

    void EnsureFormationSlotDefaults()
    {
        for (int slot = 0; slot < MaxFormationGuns; slot++)
        {
            if (formationSlotGunIndices[slot] < 0 || formationSlotGunIndices[slot] >= guns.Count)
                formationSlotGunIndices[slot] = slot < guns.Count ? slot : -1;
        }
    }

    int FindFallbackGun(int slot, List<int> active)
    {
        int defaultIndex = slot;
        if (IsUnlockedAndValid(defaultIndex) && !active.Contains(defaultIndex))
            return defaultIndex;

        for (int i = 0; i < guns.Count; i++)
        {
            if (!IsUnlockedAndValid(i)) continue;
            if (active.Contains(i)) continue;
            return i;
        }

        return -1;
    }

    bool IsUnlockedAndValid(int index)
    {
        if (index < 0 || index >= guns.Count) return false;
        if (GunShop.Instance == null) return true;
        return GunShop.Instance.IsUnlocked(index);
    }

    void TryPopulateGunDataFromSprites()
    {
#if UNITY_EDITOR
        if (gunDataList == null)
            gunDataList = new List<GunData>();

        if (gunDataList.Count >= TargetShopGunCount)
            return;

        var existingSprites = new HashSet<Sprite>();
        for (int i = 0; i < gunDataList.Count; i++)
        {
            if (gunDataList[i] != null && gunDataList[i].gunSprite != null)
                existingSprites.Add(gunDataList[i].gunSprite);
        }

        Dictionary<string, Queue<Sprite>> groupSprites = new Dictionary<string, Queue<Sprite>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> groupCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < GunGroups.Length; i++)
        {
            groupSprites[GunGroups[i]] = new Queue<Sprite>();
            groupCounts[GunGroups[i]] = 0;
        }

        for (int i = 0; i < gunDataList.Count; i++)
        {
            string g = DetectGroupFromGunData(gunDataList[i]);
            if (!string.IsNullOrEmpty(g) && groupCounts.ContainsKey(g))
                groupCounts[g]++;
        }

        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Project/Sprites/Gun" });
        List<Sprite> fallbackSprites = new List<Sprite>();
        for (int i = 0; i < spriteGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(spriteGuids[i]);
            if (string.IsNullOrEmpty(path) || path.IndexOf("/Bullet/", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null || existingSprites.Contains(sprite))
                continue;

            string group = DetectGroupFromPath(path);
            if (!string.IsNullOrEmpty(group) && groupSprites.ContainsKey(group))
                groupSprites[group].Enqueue(sprite);
            else
                fallbackSprites.Add(sprite);
        }

        foreach (string group in GunGroups)
        {
            var ordered = groupSprites[group]
                .OrderBy(s => GroupSpritePriority(group, s))
                .ThenBy(s => AssetDatabase.GetAssetPath(s), StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            groupSprites[group] = new Queue<Sprite>(ordered);
        }

        fallbackSprites = fallbackSprites
            .OrderBy(s => AssetDatabase.GetAssetPath(s), StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int added = 0;
        while (gunDataList.Count < TargetShopGunCount)
        {
            string nextGroup = SelectNextGroup(groupSprites, groupCounts);
            Sprite sprite = null;

            if (!string.IsNullOrEmpty(nextGroup))
                sprite = groupSprites[nextGroup].Dequeue();
            else if (fallbackSprites.Count > 0)
            {
                sprite = fallbackSprites[0];
                fallbackSprites.RemoveAt(0);
            }

            if (sprite == null)
                break;

            GunData template = PickTemplateForSprite(sprite);
            if (template == null)
                continue;

            GunData variant = CreateRuntimeVariant(template, sprite, gunDataList.Count + 1);
            gunDataList.Add(variant);
            existingSprites.Add(sprite);

            string addedGroup = DetectGroupFromPath(AssetDatabase.GetAssetPath(sprite));
            if (!string.IsNullOrEmpty(addedGroup) && groupCounts.ContainsKey(addedGroup))
                groupCounts[addedGroup]++;

            added++;
        }

        if (added > 0)
            Debug.Log($"[WeaponManager] Added {added} guns from Assets/Project/Sprites/Gun. Total={gunDataList.Count}");
#endif
    }

#if UNITY_EDITOR
    int GroupSpritePriority(string group, Sprite sprite)
    {
        if (sprite == null || string.IsNullOrEmpty(group))
            return 10;

        if (!group.Equals("Rifle", StringComparison.OrdinalIgnoreCase))
            return 10;

        string path = AssetDatabase.GetAssetPath(sprite) ?? string.Empty;
        string name = sprite.name ?? string.Empty;

        if (path.IndexOf("Red_Dragon", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Red_Dragon", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("Red Dragon", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Red Dragon", StringComparison.OrdinalIgnoreCase) >= 0)
            return 0;

        return 10;
    }

    string SelectNextGroup(Dictionary<string, Queue<Sprite>> groupSprites, Dictionary<string, int> groupCounts)
    {
        string selected = string.Empty;
        int minCount = int.MaxValue;

        for (int i = 0; i < GunGroups.Length; i++)
        {
            string group = GunGroups[i];
            if (!groupSprites.ContainsKey(group) || groupSprites[group].Count == 0)
                continue;

            int count = groupCounts.ContainsKey(group) ? groupCounts[group] : 0;
            if (count < minCount)
            {
                minCount = count;
                selected = group;
            }
        }

        return selected;
    }

    string DetectGroupFromGunData(GunData data)
    {
        if (data == null)
            return string.Empty;

        string name = data.gunName ?? string.Empty;
        for (int i = 0; i < GunGroups.Length; i++)
        {
            if (name.IndexOf(GunGroups[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return GunGroups[i];
        }

        if (data.gunSprite != null)
        {
            string spritePath = AssetDatabase.GetAssetPath(data.gunSprite);
            return DetectGroupFromPath(spritePath);
        }

        return string.Empty;
    }

    GunData PickTemplateForSprite(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path))
            return gunDataList.FirstOrDefault(g => g != null);

        string group = DetectGroupFromPath(path);
        if (!string.IsNullOrEmpty(group))
        {
            for (int i = 0; i < gunDataList.Count; i++)
            {
                GunData g = gunDataList[i];
                if (g == null || string.IsNullOrEmpty(g.gunName)) continue;
                if (g.gunName.IndexOf(group, StringComparison.OrdinalIgnoreCase) >= 0)
                    return g;
            }
        }

        return gunDataList.FirstOrDefault(g => g != null);
    }

    string DetectGroupFromPath(string path)
    {
        if (path.IndexOf("/Pistol/", StringComparison.OrdinalIgnoreCase) >= 0) return "Pistol";
        if (path.IndexOf("/Rifle/", StringComparison.OrdinalIgnoreCase) >= 0) return "Rifle";
        if (path.IndexOf("/Shotgun/", StringComparison.OrdinalIgnoreCase) >= 0) return "Shotgun";
        if (path.IndexOf("/Sniper/", StringComparison.OrdinalIgnoreCase) >= 0) return "Sniper";
        return string.Empty;
    }

    GunData CreateRuntimeVariant(GunData template, Sprite sprite, int serial)
    {
        GunData v = ScriptableObject.CreateInstance<GunData>();
        v.gunName = BuildRuntimeGunName(template, sprite, serial);
        v.gunSprite = sprite;

        v.bulletPrefab = ResolveBulletPrefab(template);
        v.bulletSprite = PickBulletSprite(serial - 1);
        v.damage = template.damage;
        v.bulletSpeed = template.bulletSpeed;
        v.bulletLifeTime = template.bulletLifeTime;
        v.fireRate = template.fireRate;
        v.detectRange = template.detectRange;
        v.bulletsPerShot = template.bulletsPerShot;
        v.spreadAngle = template.spreadAngle;
        v.positionOffset = template.positionOffset;
        v.muzzleFlashPrefab = template.muzzleFlashPrefab;
        v.muzzleFlashDuration = template.muzzleFlashDuration;
        v.shootSFX = template.shootSFX;

        v.unlockedByDefault = false;
        v.unlockCost = Mathf.Max(20, template.unlockCost + serial * 10);
        v.description = string.IsNullOrWhiteSpace(template.description)
            ? $"{template.gunName} variant"
            : template.description;

        return v;
    }

    string BuildRuntimeGunName(GunData template, Sprite sprite, int serial)
    {
        string raw = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sprite));
        if (string.IsNullOrEmpty(raw))
            raw = sprite.name;

        raw = raw.Replace("Sprite_", string.Empty, StringComparison.OrdinalIgnoreCase)
                 .Replace("_", " ")
                 .Trim();

        if (string.IsNullOrEmpty(raw))
            raw = template != null ? template.gunName : $"Gun {serial}";

        return raw;
    }
    GameObject ResolveBulletPrefab(GunData template)
    {
        if (template != null && template.bulletPrefab != null)
            return template.bulletPrefab;

        for (int i = 0; i < gunDataList.Count; i++)
        {
            GunData g = gunDataList[i];
            if (g != null && g.bulletPrefab != null)
                return g.bulletPrefab;
        }

        return null;
    }

    Sprite PickBulletSprite(int index)
    {
        if (cachedBulletSprites == null)
            cachedBulletSprites = LoadBulletSprites();

        if (cachedBulletSprites == null || cachedBulletSprites.Count == 0)
            return null;

        int safeIndex = Mathf.Abs(index) % cachedBulletSprites.Count;
        return cachedBulletSprites[safeIndex];
    }

    List<Sprite> LoadBulletSprites()
    {
        List<Sprite> result = new List<Sprite>();
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Project/Sprites/Gun/Bullet" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path)) continue;

            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) result.Add(sp);
        }

        return result
            .OrderBy(s => AssetDatabase.GetAssetPath(s), StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
#endif

    // ──────────────────────────────────────────────
    //  Getter (giữ để tương thích)
    // ──────────────────────────────────────────────
    public int CurrentIndex => currentIndex;
    public GunData CurrentGunData =>
        (currentIndex >= 0 && currentIndex < gunDataList.Count) ? gunDataList[currentIndex] : null;
    public int GunCount => guns.Count;

    public static void ClearSavedEquippedGun()
    {
        PlayerPrefs.DeleteKey(EquippedGunKey);
        PlayerPrefs.Save();
    }
}
