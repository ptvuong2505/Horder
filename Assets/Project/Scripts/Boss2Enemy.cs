using System.Collections;
using UnityEngine;

/// <summary>
/// Boss2Enemy
/// Script điều khiển Boss Level 2 (Prefab: Boss2).
/// 
/// Mục tiêu thiết kế:
/// - Đơn giản, dễ đọc, tự chứa (tương tự `BossEnemy` hiện có).
/// - Di chuyển và chạy animation `Boss2_Move` khi boss đang di chuyển.
/// - 3 kỹ năng:
///   1) Bắn nhiều viên theo hình “hình thang” (trapezoid) nhắm về phía player.
///   2) Bắn "quả cầu lớn" bay và thả các viên đạn nhỏ theo thời gian.
///   3) "Laser hỗn loạn" (thực chất là các viên đạn laser dài) với góc bắn đủ né được.
/// 
/// GHI CHÚ:
/// - Script này sử dụng `BossProjectile` cho toàn bộ projectile (orb/đạn/laser).
/// - Để tuỳ biến hình ảnh cho từng loại projectile, hãy tạo các prefab khác nhau
///   (khác sprite/màu/material) và gán vào các field prefab bên dưới.
/// </summary>
public class Boss2Enemy : MonoBehaviour
{
    [Header("=== CHỈ SỐ CƠ BẢN ===")]
    public int maxHealth = 1200;
    public float moveSpeed = 1.6f;
    public int contactDamage = 20;

    [Header("=== THAM CHIẾU ===")]
    [Tooltip("Không bắt buộc: gán Transform của Player; nếu để trống sẽ tự tìm theo tag Player.")]
    public Transform target;

    [Tooltip("Gán Animator nằm ở Sprites/Body (hoặc bất kỳ child nào). Dùng để chạy Boss2_Move và trigger tấn công.")]
    public Animator animator;

    [Tooltip("Tên bool trong Animator: true khi đang di chuyển. Animator Controller nên chuyển sang state Boss2_Move khi bool này true.")]
    public string moveBool = "Move";

    [Tooltip("Không bắt buộc: tên trigger animation tấn công (nếu Animator có tham số này).")]
    public string attackTrigger = "Attack";

    [Header("=== PREFAB ĐẠN (HÌNH ẢNH/HIỂN THỊ) ===")]
    [Tooltip("Prefab đạn nhỏ sử dụng cho kỹ năng 1 (multi-shot hình thang).")]
    public GameObject trapezoidBulletPrefab;

    [Tooltip("Prefab quả cầu lớn sử dụng cho kỹ năng 2.")]
    public GameObject orbPrefab;

    [Tooltip("Prefab đạn nhỏ được thả ra từ quả cầu lớn (kỹ năng 2).")]
    public GameObject orbDropBulletPrefab;

    [Tooltip("Prefab đạn laser dùng cho kỹ năng 3. Nên làm sprite dài và mỏng.")]
    public GameObject laserBulletPrefab;

    [Header("=== KỸ NĂNG 1: MULTI-SHOT HÌNH THANG ===")]
    public float skill1Cooldown = 4.0f;
    public int skill1Bursts = 3;

    // Giảm nhẹ mật độ đạn để có khe né rõ hơn.
    public int skill1BulletsPerBurst = 6;

    // Tăng nhẹ nhịp bắn (đỡ "xịt" liên tục).
    public float skill1ShotDelay = 0.12f;

    // Tăng khoảng nghỉ giữa các burst để người chơi reset vị trí.
    public float skill1BurstGap = 0.45f;

    // Thu bớt độ rộng để tránh phủ hết màn ở khoảng cách gần.
    public float skill1SpreadAngle = 48f;          // độ rộng của góc bắn
    public float skill1TrapezoidSkew = 10f;        // lệch thêm ở 2 rìa để tạo cảm giác "hình thang"
    public float skill1BulletSpeed = 7.5f;
    public int skill1BulletDamage = 10;

    [Header("=== KỸ NĂNG 2: QUẢ CẦU LỚN (THẢ ĐẠN) ===")]
    public float skill2Cooldown = 7.0f;
    public float skill2OrbSpeed = 3.6f;
    public int skill2OrbDamage = 14;
    public float skill2OrbLifeTime = 7.0f;

    // Giảm tần suất thả để màn bớt "bão đạn".
    public float skill2DropInterval = 0.25f;

    public float skill2DropSpreadAngle = 360f;

    // Giảm số viên mỗi tick.
    public int skill2DropsPerTick = 2;

    public float skill2DropBulletSpeed = 4.8f;
    public int skill2DropBulletDamage = 8;

    [Header("=== KỸ NĂNG 3: LASER CHIẾU THẲNG (TẮT SAU 1 LÚC) ===")]
    public float skill3Cooldown = 9.0f;
    public int skill3Waves = 4;

    // Giảm số lần quét để dễ né hơn nhưng vẫn áp lực.
    public int skill3LasersPerWave = 7;

    public float skill3WaveGap = 0.55f;

    [Tooltip("Góc lệch khi bắn laser (độ). Giảm nhẹ để hướng dễ đọc hơn.")]
    public float skill3AimJitterAngle = 22f;

    [Tooltip("Độ dài laser (world units).")]
    public float skill3BeamLength = 14f;

    [Tooltip("Độ dày laser (world units).")]
    public float skill3BeamThickness = 0.5f;

    [Tooltip("Thời gian laser tồn tại rồi tắt.")]
    public float skill3BeamDuration = 0.22f;

    public int skill3LaserDamage = 12;

    // Runtime (biến chạy lúc chơi)
    private int currentHealth;
    private bool isDead;

    private Rigidbody2D rb;
    private SpriteRenderer[] renderers;

    private float nextSkill1Time;
    private float nextSkill2Time;
    private float nextSkill3Time;

    public event System.Action<int, int> OnHealthChanged; // current, max
    public event System.Action OnBossDied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (animator == null)
        {
            // Ưu tiên theo convention prefab Boss2: Sprites/Body
            Transform body = transform.Find("Sprites/Body");
            if (body != null) animator = body.GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }

        // Đăng ký vào EnemyManager để hệ thống "wave clear" vẫn hoạt động đúng.
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(gameObject);

        // BossHealthBar hiện tại chỉ hỗ trợ `BossEnemy`.
        // Nếu bạn muốn Boss2 cũng hiển thị thanh máu boss, có 2 hướng:
        // 1) Mở rộng BossHealthBar để nhận một interface/event tổng quát, hoặc
        // 2) Tạo một Boss2HealthBar riêng.

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Trễ nhẹ lần cast đầu để boss không "xả" ngay lập tức khi vừa spawn.
        float t = Time.time;
        nextSkill1Time = t + 1.2f;
        nextSkill2Time = t + 2.8f;
        nextSkill3Time = t + 4.5f;
    }

    private void Update()
    {
        if (isDead) return;
        if (target == null) return;

        HandleMovement();
        HandleSkills();
    }

    private void HandleMovement()
    {
        if (rb == null) return;

        Vector2 toPlayer = (target.position - transform.position);
        Vector2 dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;

        rb.linearVelocity = dir * moveSpeed;

        if (animator != null && !string.IsNullOrWhiteSpace(moveBool))
            animator.SetBool(moveBool, rb.linearVelocity.sqrMagnitude > 0.01f);

        Flip(dir);
    }

    private void HandleSkills()
    {
        float t = Time.time;

        // Ưu tiên: laser > orb > multi-shot
        if (t >= nextSkill3Time)
        {
            nextSkill3Time = t + skill3Cooldown;
            StartCoroutine(Skill3_ChaoticLasers());
            return;
        }

        if (t >= nextSkill2Time)
        {
            nextSkill2Time = t + skill2Cooldown;
            StartCoroutine(Skill2_BigOrbDrops());
            return;
        }

        if (t >= nextSkill1Time)
        {
            nextSkill1Time = t + skill1Cooldown;
            StartCoroutine(Skill1_TrapezoidMultiShot());
        }
    }

    private IEnumerator Skill1_TrapezoidMultiShot()
    {
        if (trapezoidBulletPrefab == null || target == null) yield break;

        TriggerAttackAnim();

        for (int burst = 0; burst < skill1Bursts; burst++)
        {
            Vector2 baseDir = ((Vector2)(target.position - transform.position)).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            // Spread nhắm theo player và có cảm giác "hình thang": phần rìa mở rộng + lệch thêm nhẹ.
            for (int i = 0; i < skill1BulletsPerBurst; i++)
            {
                float t = skill1BulletsPerBurst <= 1 ? 0.5f : (float)i / (skill1BulletsPerBurst - 1);
                float center = (t - 0.5f) * 2f; // -1..1

                float angleOffset = center * (skill1SpreadAngle * 0.5f);
                angleOffset += Mathf.Sign(center) * skill1TrapezoidSkew * Mathf.Abs(center);

                float ang = baseAngle + angleOffset;
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

                SpawnBossProjectile(trapezoidBulletPrefab, transform.position, dir, skill1BulletSpeed, skill1BulletDamage);
                yield return new WaitForSeconds(skill1ShotDelay);
            }

            yield return new WaitForSeconds(skill1BurstGap);
        }
    }

    private IEnumerator Skill2_BigOrbDrops()
    {
        if (orbPrefab == null || orbDropBulletPrefab == null || target == null) yield break;

        TriggerAttackAnim();

        Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
        GameObject orb = SpawnBossProjectile(orbPrefab, transform.position, dir, skill2OrbSpeed, skill2OrbDamage);
        if (orb == null) yield break;

        // Thả đạn nhỏ khi quả cầu còn tồn tại.
        float endTime = Time.time + skill2OrbLifeTime;
        float nextDrop = Time.time;

        // Seed góc để rải đều (tránh nhiều viên ngẫu nhiên bị dồn vào cùng 1 hướng).
        float angleSeed = Random.Range(0f, 360f);

        while (orb != null && Time.time < endTime)
        {
            if (Time.time >= nextDrop)
            {
                nextDrop = Time.time + skill2DropInterval;

                // Rải tương đối đều theo vòng, thêm nhiễu nhẹ để tự nhiên.
                float step = skill2DropSpreadAngle / Mathf.Max(1, skill2DropsPerTick);
                for (int i = 0; i < skill2DropsPerTick; i++)
                {
                    float ang = angleSeed + (i * step) + Random.Range(-8f, 8f);
                    Vector2 d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                    SpawnBossProjectile(orbDropBulletPrefab, orb.transform.position, d, skill2DropBulletSpeed, skill2DropBulletDamage);
                }

                // Xoay seed để pattern thay đổi theo thời gian.
                angleSeed += 35f;
            }

            yield return null;
        }

        if (orb != null) Destroy(orb);
    }

    private IEnumerator Skill3_ChaoticLasers()
    {
        if (laserBulletPrefab == null || target == null) yield break;

        // Laser chiếu thẳng: prefab phải có `BossLaserBeam2D`.
        TriggerAttackAnim();

        for (int wave = 0; wave < skill3Waves; wave++)
        {
            Vector2 baseDir = ((Vector2)(target.position - transform.position)).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            for (int i = 0; i < skill3LasersPerWave; i++)
            {
                float jitter = Random.Range(-skill3AimJitterAngle, skill3AimJitterAngle);
                float ang = baseAngle + jitter;
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

                SpawnLaserBeam(laserBulletPrefab, transform.position, dir);

                // Nhịp nhỏ giữa các tia để người chơi đọc hướng và né.
                yield return new WaitForSeconds(0.08f);
            }

            yield return new WaitForSeconds(skill3WaveGap);
        }
    }

    private GameObject SpawnBossProjectile(GameObject prefab, Vector3 pos, Vector2 dir, float speed, int damage)
    {
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        BossProjectile bp = go.GetComponent<BossProjectile>();
        if (bp != null)
            bp.Initialize(dir, speed, damage);

        return go;
    }

    private void SpawnLaserBeam(GameObject beamPrefab, Vector3 pos, Vector2 dir)
    {
        if (beamPrefab == null) return;

        GameObject go = Instantiate(beamPrefab, pos, Quaternion.identity);
        BossLaserBeam2D beam = go.GetComponent<BossLaserBeam2D>();

        if (beam != null)
        {
            beam.Initialize(dir, skill3LaserDamage, skill3BeamLength, skill3BeamThickness, skill3BeamDuration);
        }
        else
        {
            // Fallback: nếu prefab chưa gắn BossLaserBeam2D thì vẫn bắn như projectile cũ để không "im luôn".
            BossProjectile bp = go.GetComponent<BossProjectile>();
            if (bp != null)
                bp.Initialize(dir, 10f, skill3LaserDamage);
        }
    }

    private void TriggerAttackAnim()
    {
        if (animator == null) return;

        if (!string.IsNullOrWhiteSpace(attackTrigger))
            animator.SetTrigger(attackTrigger);
    }

    private void Flip(Vector2 dir)
    {
        // (Tuỳ chọn) Lật theo trục X giống Enemy/Boss hiện có.
        if (dir.x == 0) return;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir.x < 0 ? -1 : 1);
        transform.localScale = s;
    }

    // ──────────────────────────────────────────────
    // Damage / Death
    // ──────────────────────────────────────────────
    public void Hit(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Unregister if system exists.
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(gameObject);

        OnBossDied?.Invoke();
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.collider != null && collision.collider.CompareTag("Player"))
        {
            Player p = collision.collider.GetComponent<Player>();
            if (p != null)
                p.Hit(contactDamage);
        }
    }

    private void SetColor(Color c)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r != null) r.color = c;
        }
    }
}
