using System.Collections;
using UnityEngine;

/// <summary>
/// PickupItem – Item nhặt được: hồi HP, tăng damage, tăng speed, coin...
/// Gắn lên prefab pickup item.
/// </summary>
public class PickupItem : MonoBehaviour
{
    public enum PickupType { HealthPotion, DamageBoost, SpeedBoost }

    [Header("Config")]
    public PickupType pickupType = PickupType.HealthPotion;
    public int healAmount = 25;
    public float boostDuration = 10f;
    public float boostValue = 1.5f;     // Multiplier cho damage/speed boost
    public float lifetime = 15f;         // Tự hủy sau X giây

    [Header("Visual")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Bobbing animation
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        Player player = collision.GetComponent<Player>();
        if (player == null) return;

        switch (pickupType)
        {
            case PickupType.HealthPotion:
                player.Heal(healAmount);
                break;

            case PickupType.DamageBoost:
                // Buff damage tạm thời cho tất cả súng
                WeaponManager wm = player.GetComponent<WeaponManager>();
                if (wm != null)
                {
                    foreach (var gun in wm.guns)
                    {
                        if (gun != null)
                            gun.damageMultiplier *= boostValue;
                    }
                    // Reset sau duration
                    player.StartCoroutine(ResetDamageBoost(wm, boostValue, boostDuration));
                }
                break;

            case PickupType.SpeedBoost:
                float originalSpeed = player.speed;
                player.speed *= boostValue;
                player.StartCoroutine(ResetSpeedBoost(player, originalSpeed, boostDuration));
                break;
        }

        // SFX
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup();

        Destroy(gameObject);
    }

    IEnumerator ResetDamageBoost(WeaponManager wm, float multiplier, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (wm != null)
        {
            foreach (var gun in wm.guns)
            {
                if (gun != null)
                    gun.damageMultiplier /= multiplier;
            }
        }
    }

    IEnumerator ResetSpeedBoost(Player player, float originalSpeed, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (player != null)
            player.speed = originalSpeed;
    }
}
