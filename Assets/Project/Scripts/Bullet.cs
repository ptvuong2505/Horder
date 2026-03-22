using UnityEngine;

/// <summary>
/// Bullet
/// - Bay theo trục local "right" (transform.right).
/// - Va chạm Enemy/BossEnemy thì gọi Hit(damage) rồi tự hủy.
/// 
/// Mở rộng về sau:
/// - Thêm xuyên mục tiêu (pierce), nổ AOE, hiệu ứng burn/poison/freeze...
/// - Dùng layer mask thay vì tag để tối ưu/ổn định hơn.
/// </summary>
public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 2f;
    public Vector2 direction = Vector2.right;

    // damage có thể set khác nhau cho từng loại súng / prefab
    public int damage = 10;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : (Vector2)transform.right;
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Gây damage cho Enemy nếu có component Enemy
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Hit(damage);
            }

            // Gây damage cho BossEnemy nếu có component BossEnemy
            BossEnemy boss = collision.GetComponent<BossEnemy>();
            if (boss != null)
            {
                boss.Hit(damage);
            }

            Destroy(gameObject);
        }
    }
}
