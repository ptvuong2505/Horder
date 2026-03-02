using UnityEngine;

/// <summary>
/// PickupSpawner – Quản lý spawn pickup items khi enemy chết.
/// Đặt static methods để Enemy.Die() gọi trực tiếp.
/// Cần đặt 1 instance trong scene hoặc trên GameManager.
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
