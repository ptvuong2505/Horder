using UnityEngine;

/// <summary>
/// EnemyProjectile – Đạn do enemy bắn ra, gây damage cho Player.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifeTime = 3f;
    public int damage = 6;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);

        // Nếu có Rigidbody2D thì đẩy velocity ngay từ đầu để đạn bay ổn định,
        // tránh trường hợp bị đứng yên do hierarchy/parent hoặc do physics settings.
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = (Vector2)transform.right * speed;
        }
    }

    void Update()
    {
        // Nếu không dùng Rigidbody2D thì tự di chuyển bằng transform.
        if (rb == null)
            transform.position += transform.right * speed * Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Nếu có Rigidbody2D thì giữ velocity mỗi tick physics (phòng việc bị drag/constraints làm về 0).
        if (rb != null)
            rb.linearVelocity = (Vector2)transform.right * speed;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
                player.Hit(damage);

            Destroy(gameObject);
        }
    }
}
