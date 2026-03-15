using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa từng loại súng.
/// Tạo asset: chuột phải > Create > Game/Gun Data
/// </summary>
[CreateAssetMenu(fileName = "GunData", menuName = "Game/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Info")]
    public string gunName = "Pistol";
    public Sprite gunSprite;              // Sprite hiển thị trên UI slot súng

    [Header("Bullet")]
    public GameObject bulletPrefab;       // Prefab đạn riêng cho từng khẩu
    public int damage = 10;
    public float bulletSpeed = 12f;
    public float bulletLifeTime = 2f;

    [Header("Fire Config")]
    public float fireRate = 0.5f;         // Giây giữa 2 phát bắn
    public float detectRange = 10f;       // Bán kính tìm enemy gần nhất
    public int bulletsPerShot = 1;        // Số đạn mỗi lần bắn (shotgun = nhiều)
    public float spreadAngle = 0f;        // Góc tản mát (độ), shotgun nên đặt 30-45

    [Header("Position Offset")]
    public Vector2 positionOffset = new Vector2(1f, 0f); // Offset so với Player

    [Header("Effects")]
    public GameObject muzzleFlashPrefab;  // Hiệu ứng lửa nòng, có thể để trống
    public float muzzleFlashDuration = 0.05f;

    [Header("Audio")]
    public AudioClip shootSFX;            // SFX riêng cho từng khẩu, nếu null sẽ dùng default
}
