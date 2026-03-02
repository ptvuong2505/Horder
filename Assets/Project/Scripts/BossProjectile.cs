using UnityEngine;

/// <summary>
/// BossProjectile – Đạn boss bắn ra.
/// Khác đạn enemy thường: có thể xuyên qua enemy, chỉ damage Player.
/// Tự tạo prefab: Sprite tròn nhỏ + CircleCollider2D (isTrigger) + Rigidbody2D (Kinematic)
/// </summary>
public class BossProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private float lifeTime = 5f;

    public void Initialize(Vector2 dir, float spd, int dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;

        // Xoay sprite theo hướng bắn
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.Hit(damage);
            }
            Destroy(gameObject);
        }
    }
}
