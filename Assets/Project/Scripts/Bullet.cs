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
        // Trước đây đạn chỉ xử lý khi tag = "Enemy".
        // Boss2 đang để tag Untagged nên sẽ không bao giờ nhận damage.
        // Giải pháp: ưu tiên check component để không phụ thuộc tag.

        bool didHitSomething = false;

        // Enemy thường
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Hit(damage);
            didHitSomething = true;
        }

        // Boss level 1
        BossEnemy boss1 = collision.GetComponent<BossEnemy>();
        if (boss1 != null)
        {
            boss1.Hit(damage);
            didHitSomething = true;
        }

        // Boss level 2
        Boss2Enemy boss2 = collision.GetComponent<Boss2Enemy>();
        if (boss2 != null)
        {
            boss2.Hit(damage);
            didHitSomething = true;
        }

        if (didHitSomething)
            Destroy(gameObject);
    }
}
