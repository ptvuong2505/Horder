using UnityEngine;

/// <summary>
/// PickupSpawner (Singleton)
/// Quản lý tỉ lệ rơi item.
/// 
/// Cách hoạt động:
/// - Enemy.Die() gọi PickupSpawner.TrySpawnPickup(pos)
/// - SpawnPickup() roll 0..100 rồi đi theo thứ tự cumulative (health -> damage -> speed)
///   => Các chance là "tương đối theo thứ tự" chứ không phải roll riêng từng loại.
/// 
/// Ví dụ:
/// - health=15, damage=5, speed=5
/// - roll 0..15: ra health
/// - roll 15..20: ra damage
/// - roll 20..25: ra speed
/// - còn lại: không drop
/// </summary>
public class PickupSpawner : MonoBehaviour
{
    public static PickupSpawner Instance;

    [Header("Pickup Prefabs — gán trong Inspector")]
    public GameObject healthPotionPrefab;
    public GameObject damageBoostPrefab;
    public GameObject speedBoostPrefab;

    [Header("Drop Chances (0-100%)")]
    [Range(0, 100)] public float healthDropChance = 15f;
    [Range(0, 100)] public float damageDropChance = 5f;
    [Range(0, 100)] public float speedDropChance = 5f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    /// <summary>
    /// Gọi từ Enemy.Die() — thử spawn pickup tại vị trí enemy chết.
    /// </summary>
    public static void TrySpawnPickup(Vector3 position)
    {
        if (Instance == null) return;
        Instance.SpawnPickup(position);
    }

    void SpawnPickup(Vector3 position)
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        // Health potion
        cumulative += healthDropChance;
        if (roll < cumulative && healthPotionPrefab != null)
        {
            Instantiate(healthPotionPrefab, position, Quaternion.identity);
            return;
        }

        // Damage boost
        cumulative += damageDropChance;
        if (roll < cumulative && damageBoostPrefab != null)
        {
            Instantiate(damageBoostPrefab, position, Quaternion.identity);
            return;
        }

        // Speed boost
        cumulative += speedDropChance;
        if (roll < cumulative && speedBoostPrefab != null)
        {
            Instantiate(speedBoostPrefab, position, Quaternion.identity);
            return;
        }

        // Không drop gì
    }
}
