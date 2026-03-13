using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UpgradeManager (Singleton)
/// Hệ upgrade "trong run":
/// - Khi GameManager báo WaveClear -> ShowUpgradeSelection()
/// - Random 3 UpgradeData, pause game (Time.timeScale = 0)
/// - Player chọn 1 card -> ApplyUpgrade() -> resume
/// 
/// IsSelecting:
/// - Được EnemySpawner dùng để "đợi" người chơi chọn xong trước khi sang wave.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Upgrade Pool — gán tất cả UpgradeData assets")]
    public List<UpgradeData> upgradePool = new List<UpgradeData>();

    [Header("UI")]
    public UpgradeUI upgradeUI;

    // Event: gọi khi player đã chọn upgrade xong
    public event Action OnUpgradeSelected;

    private bool isSelectingUpgrade = false;
    public bool IsSelecting => isSelectingUpgrade;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    // ──────────────────────────────────────────────
    //  Gọi từ GameManager khi wave clear
    // ──────────────────────────────────────────────
    public void ShowUpgradeSelection()
    {
        if (upgradePool.Count == 0 || upgradeUI == null)
        {
            // Không có upgrade → bỏ qua
            OnUpgradeSelected?.Invoke();
            return;
        }

        isSelectingUpgrade = true;

        // Pick 3 random upgrades (không trùng)
        List<UpgradeData> options = GetRandomUpgrades(3);

        // Pause game
        Time.timeScale = 0f;

        // Hiện UI
        upgradeUI.ShowCards(options, OnCardClicked);
    }

    List<UpgradeData> GetRandomUpgrades(int count)
    {
        List<UpgradeData> pool = new List<UpgradeData>(upgradePool);
        List<UpgradeData> result = new List<UpgradeData>();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    // ──────────────────────────────────────────────
    //  Khi player click vào 1 card
    // ──────────────────────────────────────────────
    void OnCardClicked(UpgradeData selected)
    {
        ApplyUpgrade(selected);

        // Ẩn UI
        upgradeUI.HideCards();

        // Resume game
        Time.timeScale = 1f;
        isSelectingUpgrade = false;

        // SFX
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUpgrade();

        // Thông báo đã chọn xong
        OnUpgradeSelected?.Invoke();
    }

    // ──────────────────────────────────────────────
    //  Áp dụng upgrade cho Player / Weapons
    // ──────────────────────────────────────────────
    void ApplyUpgrade(UpgradeData data)
    {
        Player player = FindFirstObjectByType<Player>();
        WeaponManager wm = player != null ? player.GetComponent<WeaponManager>() : null;

        switch (data.upgradeType)
        {
            case UpgradeData.UpgradeType.MaxHP:
                if (player != null)
                    player.AddMaxHP(Mathf.RoundToInt(data.value));
                break;

            case UpgradeData.UpgradeType.HealHP:
                if (player != null)
                    player.Heal(Mathf.RoundToInt(data.value));
                break;

            case UpgradeData.UpgradeType.MoveSpeed:
                if (player != null)
                    player.AddSpeed(data.value);
                break;

            case UpgradeData.UpgradeType.Damage:
                if (wm != null)
                {
                    foreach (var gun in wm.guns)
                    {
                        if (gun != null)
                            gun.damageMultiplier += data.value;
                    }
                }
                break;

            case UpgradeData.UpgradeType.FireRate:
                // Giảm fire rate (value < 1 = bắn nhanh hơn)
                if (wm != null)
                {
                    foreach (var gun in wm.guns)
                    {
                        if (gun != null)
                            gun.fireRateMultiplier *= data.value;
                    }
                }
                break;

            case UpgradeData.UpgradeType.BulletSpeed:
                // Không có runtime modifier trực tiếp → skip cho giờ
                Debug.Log($"[UpgradeManager] BulletSpeed upgrade +{data.value} — chưa implement");
                break;

            case UpgradeData.UpgradeType.DetectRange:
                // Không có runtime modifier trực tiếp → skip cho giờ
                Debug.Log($"[UpgradeManager] DetectRange upgrade +{data.value} — chưa implement");
                break;
        }

        Debug.Log($"[UpgradeManager] Áp dụng upgrade: {data.upgradeName} ({data.upgradeType} +{data.value})");
    }
}
