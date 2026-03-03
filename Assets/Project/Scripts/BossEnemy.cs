using System.Collections;
using UnityEngine;

/// <summary>
/// BossEnemy – Script chính cho boss.
/// Gắn vào Boss prefab thay vì Enemy.cs
/// 
/// Hành vi:
/// Phase 1 (HP > 50%): Di chuyển chậm về phía player, bắn đạn xung quanh (radial burst)
/// Phase 2 (HP 50%-25%): Tăng tốc, thêm dash attack, bắn nhanh hơn
/// Phase 3 ENRAGE (HP < 25%): Cuồng nộ - đỏ, bắn spiral + summon minion
/// </summary>
public class BossEnemy : MonoBehaviour
{
    [Header("=== STATS ===")]
    public int maxHealth = 800;
    public float moveSpeed = 1.2f;
    public int contactDamage = 15;

    [Header("=== RADIAL BURST (Bắn xung quanh) ===")]
    public GameObject projectilePrefab;
    public int bulletsPerBurst = 12;          // Số đạn mỗi lần bắn tròn
    public float burstCooldown = 3f;          // Giây giữa 2 lần burst
    public float projectileSpeed = 5f;
    public int projectileDamage = 8;

    [Header("=== DASH ATTACK (Phase 2+) ===")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.4f;
    public float dashCooldown = 5f;
    public int dashDamage = 20;

    [Header("=== ENRAGE (Phase 3) ===")]
    public float enrageSpeedMultiplier = 1.5f;
    public float enrageBurstCooldownMultiplier = 0.5f;
    public int spiralBullets = 8;
    public float spiralRotationSpeed = 30f;   // Độ/giây xoay spiral

    [Header("=== SUMMON (Phase 3) ===")]
    public GameObject minionPrefab;           // Gán Enemy_Lv1 prefab
    public int minionsPerSummon = 3;
    public float summonCooldown = 10f;

    // ─── Runtime ───
    private int currentHealth;
    private bool isDead = false;
    private bool isEnraged = false;
    private bool isDashing = false;

    private Transform target;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer[] renderers;

    // Cooldown timers
    private float lastBurstTime = -999f;
    private float lastDashTime = -999f;
    private float lastSummonTime = -999f;
    private float spiralAngle = 0f;

    // Phase tracking
    private int currentPhase = 1;

    // Events
    public event System.Action<int, int> OnHealthChanged;  // current, max
    public event System.Action OnBossDied;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Tìm Animator
        Transform bodyTransform = transform.Find("Sprites/Body");
        if (bodyTransform != null)
            anim = bodyTransform.GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        renderers = GetComponentsInChildren<SpriteRenderer>();

        // Tìm player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;

        // Đăng ký với EnemyManager
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(gameObject);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isDead || target == null) return;

        // Cập nhật phase dựa trên HP
        UpdatePhase();

        // Di chuyển
        if (!isDashing)
        {
            float speed = isEnraged ? moveSpeed * enrageSpeedMultiplier : moveSpeed;
            Vector3 dir = (target.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(dir.x * speed, dir.y * speed);
            Flip(dir);
        }

        // Attacks
        HandleAttacks();
    }

    // ═══════════════════════════════════════════════
    //  PHASE SYSTEM
    // ═══════════════════════════════════════════════
    void UpdatePhase()
    {
        float hpPercent = (float)currentHealth / maxHealth;

        if (hpPercent <= 0.25f && currentPhase < 3)
        {
            currentPhase = 3;
            EnterEnrage();
        }
        else if (hpPercent <= 0.5f && currentPhase < 2)
        {
            currentPhase = 2;
            Debug.Log("[Boss] Phase 2! Tăng tốc + Dash Attack");
        }
    }

    void EnterEnrage()
    {
        isEnraged = true;
        Debug.Log("[Boss] ENRAGE! Cuồng nộ!");

        // Visual: đổi màu đỏ + scale lên
        StartCoroutine(EnrageVisual());
    }

    IEnumerator EnrageVisual()
    {
        // Flash đỏ 3 lần    
        for (int i = 0; i < 3; i++)
        {
            SetColor(Color.red);
            yield return new WaitForSeconds(0.15f);
            SetColor(Color.white);
            yield return new WaitForSeconds(0.15f);
        }
        // Giữ màu đỏ nhạt
        SetColor(new Color(1f, 0.5f, 0.5f, 1f));

        // Scale lên nhẹ
        transform.localScale *= 1.15f;
    }

    // ═══════════════════════════════════════════════
    //  ATTACKS
    // ═══════════════════════════════════════════════
    void HandleAttacks()
    {
        float currentBurstCooldown = isEnraged ? burstCooldown * enrageBurstCooldownMultiplier : burstCooldown;

        // Radial Burst
        if (Time.time >= lastBurstTime + currentBurstCooldown)
        {
            if (isEnraged)
                StartCoroutine(SpiralAttack());
            else
                RadialBurst();
            lastBurstTime = Time.time;
        }

        // Dash Attack (Phase 2+)
        if (currentPhase >= 2 && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            StartCoroutine(DashAttack());
            lastDashTime = Time.time;
        }

        // Summon Minions (Phase 3)
        if (currentPhase >= 3 && Time.time >= lastSummonTime + summonCooldown)
        {
            SummonMinions();
            lastSummonTime = Time.time;
        }
    }

    // ─── Radial Burst: Bắn đạn tỏa tròn ───
    void RadialBurst()
    {
        if (projectilePrefab == null) return;

        if (anim != null) anim.SetTrigger("Attack");

        float angleStep = 360f / bulletsPerBurst;
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            GameObject bullet = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            BossProjectile bp = bullet.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.Initialize(dir, projectileSpeed, projectileDamage);
            }
        }
    }

    // ─── Spiral Attack: Bắn xoáy (Enrage) ───
    IEnumerator SpiralAttack()
    {
        if (projectilePrefab == null) yield break;

        if (anim != null) anim.SetTrigger("Attack");

        int totalShots = spiralBullets * 3; // 3 vòng quay
        float shotDelay = 0.08f;

        for (int i = 0; i < totalShots; i++)
        {
            spiralAngle += spiralRotationSpeed;
            Vector2 dir = new Vector2(
                Mathf.Cos(spiralAngle * Mathf.Deg2Rad),
                Mathf.Sin(spiralAngle * Mathf.Deg2Rad)
            );

            GameObject bullet = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            BossProjectile bp = bullet.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.Initialize(dir, projectileSpeed * 0.8f, projectileDamage);
            }

            yield return new WaitForSeconds(shotDelay);
        }
    }

    // ─── Dash Attack: Lao nhanh về phía player ───
    IEnumerator DashAttack()
    {
        if (target == null) yield break;

        isDashing = true;

        if (anim != null) anim.SetTrigger("Attack");

        // Flash trắng trước khi dash (cảnh báo)
        SetColor(Color.yellow);
        yield return new WaitForSeconds(0.3f);
        SetColor(isEnraged ? new Color(1f, 0.5f, 0.5f, 1f) : Color.white);

        // Dash
        Vector2 dashDir = ((Vector2)(target.position - transform.position)).normalized;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }

    // ─── Summon Minions ───
    void SummonMinions()
    {
        if (minionPrefab == null) return;

        Debug.Log($"[Boss] Triệu hồi {minionsPerSummon} quân lính!");

        for (int i = 0; i < minionsPerSummon; i++)
        {
            float angle = (360f / minionsPerSummon) * i;
            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * 2f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * 2f
            );
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
            Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        }
    }

    // ═══════════════════════════════════════════════
    //  DAMAGE / DEATH
    // ═══════════════════════════════════════════════
    public void Hit(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Flash hit
        StartCoroutine(HitFlash());

        if (anim != null)
            anim.SetTrigger("Hit");

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator HitFlash()
    {
        SetColor(Color.white);
        yield return new WaitForSeconds(0.05f);
        SetColor(isEnraged ? new Color(1f, 0.5f, 0.5f, 1f) : Color.white);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[Boss] DEFEATED!");

        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Audio
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDie();

        OnBossDied?.Invoke();

        // Score
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(200);

        // Death effect
        StartCoroutine(DeathEffect());
    }

    IEnumerator DeathEffect()
    {
        // Nhấp nháy + phóng to rồi biến mất
        for (int i = 0; i < 8; i++)
        {
            SetColor(i % 2 == 0 ? Color.red : Color.white);
            transform.localScale *= 1.03f;
            yield return new WaitForSeconds(0.1f);
        }

        // Fade out
        float duration = 0.5f;
        float elapsed = 0f;
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

    // ═══════════════════════════════════════════════
    //  COLLISION (Damage on contact)
    // ═══════════════════════════════════════════════
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                int dmg = isDashing ? dashDamage : contactDamage;
                player.Hit(dmg);
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead || isDashing) return;
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
                player.Hit(contactDamage);
        }
    }

    // ═══════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════
    void Flip(Vector3 dir)
    {
        if (dir.x != 0)
            transform.localScale = new Vector3(
                Mathf.Sign(-dir.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
    }

    void SetColor(Color color)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
            r.color = color;
    }

    // Public getters
    public float HealthPercent => (float)currentHealth / maxHealth;
    public int CurrentPhase => currentPhase;
    public bool IsEnraged => isEnraged;
}
