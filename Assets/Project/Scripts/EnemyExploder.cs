using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyExploder – Enemy chạy nhanh về phía Player, nổ AoE khi đến gần.
/// Gắn lên prefab enemy Exploder thay cho script Enemy.cs thông thường.
/// </summary>
public class EnemyExploder : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 30;
    public float speed = 4.5f;            // Nhanh hơn enemy thường
    public float explosionRange = 2f;   // Khoảng cách kích nổ
    public int explosionDamage = 25;
    public float explosionRadius = 2.5f;  // Bán kính AoE

    private int currentHealth;
    private bool isDead = false;
    private bool hasExploded = false;

    Animator anim;
    Rigidbody2D rb;
    Transform target;

    void Start()
    {
        currentHealth = maxHealth;

        Transform bodyTransform = transform.Find("Sprites/Body");
        if (bodyTransform != null)
            anim = bodyTransform.GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(gameObject);
    }

    void Update()
    {
        if (isDead || target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        Vector3 direction = (target.position - transform.position).normalized;

        if (dist <= explosionRange)
        {
            // Đến gần đủ → NỔ
            Explode();
            return;
        }

        // Chạy nhanh về phía player
        if (rb != null)
            rb.linearVelocity = new Vector2(direction.x * speed, direction.y * speed);

        Flip(direction);
    }

    public void Hit(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (anim != null)
            anim.SetTrigger("Hit");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyHit();

        if (currentHealth <= 0)
        {
            // Chết trước khi kịp nổ → cũng nổ nhưng damage giảm
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        isDead = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // SFX nổ
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayExplosion();

        // Gây AoE damage cho Player nếu trong bán kính
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float dist = Vector2.Distance(transform.position, playerObj.transform.position);
            if (dist <= explosionRadius)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null)
                    player.Hit(explosionDamage);
            }
        }

        // Đăng ký kill
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKill();

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(gameObject);

        PickupSpawner.TrySpawnPickup(transform.position);

        // Hiệu ứng nổ
        StartCoroutine(ExplosionEffect());
    }

    IEnumerator ExplosionEffect()
    {
        // Flash trắng rồi scale up rồi fade out
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        // Scale lên nhanh
        float scaleTime = 0.15f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleTime;
            transform.localScale = originalScale * Mathf.Lerp(1f, 2.5f, t);

            // Flash trắng
            foreach (var r in renderers)
                r.color = Color.Lerp(Color.white, new Color(1, 0.5f, 0, 1), t);

            yield return null;
        }

        // Fade out
        float fadeDuration = 0.2f;
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            foreach (var r in renderers)
            {
                Color c = r.color;
                c.a = alpha;
                r.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    public void Flip(Vector3 dir)
    {
        if (dir.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(-dir.x), 1, 1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
