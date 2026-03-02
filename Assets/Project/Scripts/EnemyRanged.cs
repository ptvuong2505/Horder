using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyRanged – Enemy giữ khoảng cách và bắn projectile về phía Player.
/// Gắn lên prefab enemy Ranged thay cho script Enemy.cs thông thường.
/// </summary>
public class EnemyRanged : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 45;
    public float speed = 1.5f;
    public float attackRange = 7f;
    public float fireRate = 2f;
    public int damageToPlayer = 6;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    private int currentHealth;
    private bool isDead = false;
    private float fireCooldown = 0f;

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

        if (dist > attackRange)
        {
            // Di chuyển về phía player
            if (rb != null)
                rb.linearVelocity = new Vector2(direction.x * speed, direction.y * speed);
        }
        else
        {
            // Dừng lại và bắn
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f)
            {
                Fire(direction);
                fireCooldown = fireRate;
            }
        }

        Flip(direction);
    }

    void Fire(Vector3 direction)
    {
        if (projectilePrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);
        GameObject proj = Instantiate(projectilePrefab, transform.position, rot);

        EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
        if (ep != null)
            ep.damage = damageToPlayer;
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
            Die();
    }

    public void Flip(Vector3 dir)
    {
        if (dir.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(-dir.x), 1, 1);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDie();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKill();

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(gameObject);

        PickupSpawner.TrySpawnPickup(transform.position);

        StartCoroutine(DeathEffect());
    }

    IEnumerator DeathEffect()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
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
}
