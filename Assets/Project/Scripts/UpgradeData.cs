using UnityEngine;

/// <summary>
/// UpgradeData – ScriptableObject định nghĩa từng loại upgrade.
/// Tạo asset: chuột phải > Create > Game/Upgrade Data
/// </summary>
[CreateAssetMenu(fileName = "UpgradeData", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public enum UpgradeType
    {
        MaxHP,         // Tăng HP tối đa
        MoveSpeed,     // Tăng tốc di chuyển
        Damage,        // Tăng damage tất cả súng
        FireRate,      // Giảm thời gian giữa 2 phát bắn
        BulletSpeed,   // Tăng tốc đạn
        DetectRange,   // Tăng tầm phát hiện enemy
        HealHP         // Hồi HP ngay lập tức
    }

    [Header("Info")]
    public string upgradeName = "Upgrade";
    [TextArea(2, 4)]
    public string description = "Mô tả upgrade";
    public Sprite icon;

    [Header("Effect")]
    public UpgradeType upgradeType;
    public float value = 10f;            // Giá trị: +HP, +speed, damage multiplier, ...
}
