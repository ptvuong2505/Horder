using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth;
    public float speed;
    public int damageToPlayer = 10;

    private int currentHealth;
    private bool isDead = false;

    Animator anim;
    Rigidbody2D rb;

    Transform target;

    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;        // Tìm đúng Animator trên child "Body" (có trigger Hit)
        // GetComponentInChildren có thể lấy nhầm Animator khác
        Transform bodyTransform = transform.Find("Sprites/Body");
        if (bodyTransform != null)
            anim = bodyTransform.GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>(); // fallback

        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;

        // Đăng ký với EnemyManager
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(gameObject);
    }

    void Update()
    {
        if (isDead) return;

        if (target != null && rb != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * speed, direction.y * speed);
            Flip(direction);
        }
    }

    public void Hit(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (anim != null)
            anim.SetTrigger("Hit");

        if (currentHealth <= 0)
            Die();
    }

    public void Flip(Vector3 dir)
    {
        if (dir.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(-dir.x), 1, 1
            );
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Dừng di chuyển
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Tắt collider để không bị tấn công / tấn công player nữa
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Báo GameManager cộng điểm + coins
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKill();

        // Báo EnemyManager giảm bộ đếm wave
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(gameObject);

        // Chạy hiệu ứng chết (fade out sprite)
        StartCoroutine(DeathEffect());
    }

    IEnumerator DeathEffect()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        // Thu thập tất cả SpriteRenderer trong object (Body + Hit child)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        // Lưu màu gốc
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = originalColors[i];
                c.a = alpha;
                renderers[i].color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Player"))
            AttackPlayer(collision.gameObject);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
                AttackPlayer(collision.gameObject);
        }
    }

    void AttackPlayer(GameObject playerObject)
    {
        Player player = playerObject.GetComponent<Player>();
        if (player != null)
        {
            player.Hit(damageToPlayer);
            lastAttackTime = Time.time;
        }
    }
}