using System.Collections;
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// AutoGun
/// "Súng tự động" xoay theo enemy gần nhất và tự bắn theo fireRate.
/// 
/// Luồng hoạt động:
/// - WeaponManager.Setup() gọi Setup(GunData) để gán cấu hình súng.
/// - Update(): tìm target gần nhất trong bán kính detectRange, xoay hướng, bắn theo cooldown.
/// - Damage/FireRate có modifier runtime (damageMultiplier, fireRateMultiplier) để upgrade/pickup tác động.
/// 
/// Lưu ý hiệu năng:
/// - FindNearestEnemy() hiện dùng GameObject.FindGameObjectsWithTag("Enemy") mỗi frame.
///   Khi enemy đông sẽ hao CPU/GC.
///   Hướng nâng cấp: EnemyManager cung cấp danh sách/nearby query hoặc dùng Physics2D.OverlapCircleNonAlloc.
/// </summary>
public class AutoGun : MonoBehaviour
{
    [Header("Gun Data")]
    public GunData gunData;

    [Header("References")]
    public Transform muzzlePosition;    // Đầu nòng súng (điểm spawn đạn)
    public GameObject muzzleFlash;      // Hiệu ứng lửa nòng (SpriteRenderer / Particle)
    public SpriteRenderer gunRenderer;  // Renderer của thân súng

    private float fireCooldown = 0f;
    private Transform currentTarget;
    private static bool micLookupInitialized;
    private static Type micManagerType;
    private static FieldInfo micInstanceField;
    private static MethodInfo micBoostMethod;

    // Multipliers cho upgrade/pickup (mặc định = 1)
    [HideInInspector] public float damageMultiplier = 1f;
    [HideInInspector] public float fireRateMultiplier = 1f;    // Gọi từ WeaponManager để truyền GunData vào
    public void Setup(GunData data)
    {
        gunData = data;
        fireCooldown = 0f;

        if (gunData != null)
        {
            transform.localPosition = gunData.positionOffset;
            transform.localRotation = Quaternion.identity;
            ApplyVisualFromData();
        }

        Debug.Log($"[AutoGun] {gameObject.name} đã nhận GunData: {data?.gunName}");
    }

    void Start()
    {
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    void OnEnable()
    {
        // Tránh trường hợp renderer bị tắt từ prefab/animation làm súng không hiển thị.
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = true;
        }

        if (gunData != null)
        {
            transform.localPosition = gunData.positionOffset;
            ApplyVisualFromData();
        }
    }

    void ApplyVisualFromData()
    {
        if (gunRenderer == null)
            gunRenderer = GetComponent<SpriteRenderer>();

        if (gunRenderer == null || gunData == null)
            return;

        if (gunData.gunSprite != null)
            gunRenderer.sprite = gunData.gunSprite;

        // Đồng bộ sorting với body player để súng không bị chìm dưới layer khác.
        Player player = GetComponentInParent<Player>();
        if (player != null && player.body != null)
        {
            gunRenderer.sortingLayerID = player.body.sortingLayerID;
            gunRenderer.sortingOrder = player.body.sortingOrder + 1;
        }

        gunRenderer.enabled = true;
        Color c = gunRenderer.color;
        c.a = 1f;
        gunRenderer.color = c;
    }

    void Update()
    {
        if (gunData == null) return;

        // Áp dụng Microphone multiplier
        float micBoost = GetMicBoostSafe();

        fireCooldown -= (Time.deltaTime * micBoost);

        currentTarget = FindNearestEnemy();

        if (currentTarget != null)
        {
            RotateTowardsTarget(currentTarget);

            if (fireCooldown <= 0f)
            {
                Fire();
                fireCooldown = gunData.fireRate * fireRateMultiplier;
            }
        }
    }

    float GetMicBoostSafe()
    {
        if (!micLookupInitialized)
        {
            micLookupInitialized = true;
            micManagerType = Type.GetType("MicInputManager") ?? Type.GetType("MicInputManager, Assembly-CSharp");
            if (micManagerType != null)
            {
                micInstanceField = micManagerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                micBoostMethod = micManagerType.GetMethod("GetVolumeBoost", BindingFlags.Public | BindingFlags.Instance);
            }
        }

        if (micManagerType == null || micInstanceField == null || micBoostMethod == null)
            return 1f;

        object instance = micInstanceField.GetValue(null);
        if (instance == null)
            return 1f;

        object result = micBoostMethod.Invoke(instance, null);
        return result is float value ? value : 1f;
    }

    Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float minDist = gunData.detectRange;

        foreach (var e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e.transform;
            }
        }
        return nearest;
    }

    void RotateTowardsTarget(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle += GetAimAngleOffset();
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    float GetAimAngleOffset()
    {
        if (gunData == null || string.IsNullOrEmpty(gunData.gunName))
            return 0f;

        string name = gunData.gunName;
        if (name.IndexOf("Shotgun", StringComparison.OrdinalIgnoreCase) >= 0)
            return 180f;
        if (name.IndexOf("Sniper", StringComparison.OrdinalIgnoreCase) >= 0)
            return 180f;

        return 0f;
    }

    void Fire()
    {
        if (gunData.bulletPrefab == null)
        {
            Debug.LogWarning($"[AutoGun] {gameObject.name}: bulletPrefab chưa gán trong GunData!");
            return;
        }

        Vector3 aimDirection = (currentTarget != null)
            ? (currentTarget.position - transform.position).normalized
            : transform.right;
        if (aimDirection.sqrMagnitude < 0.0001f)
            aimDirection = transform.right;

        Vector3 spawnPos = ResolveMuzzleSpawnPosition(aimDirection);
        Quaternion baseRot = Quaternion.FromToRotation(Vector3.right, aimDirection);

        if (gunData.bulletsPerShot <= 1)
        {
            SpawnBullet(spawnPos, baseRot, 0f);
        }
        else
        {
            float totalSpread = gunData.spreadAngle;
            float step = totalSpread / (gunData.bulletsPerShot - 1);
            float startAngle = -totalSpread / 2f;

            for (int i = 0; i < gunData.bulletsPerShot; i++)
            {
                SpawnBullet(spawnPos, baseRot, startAngle + step * i);
            }
        }

        if (muzzleFlash != null)
            StartCoroutine(ShowMuzzleFlash());

        // Phát SFX bắn
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShoot(gunData.shootSFX);
    }

    Vector3 ResolveMuzzleSpawnPosition(Vector3 aimDirection)
    {
        Vector3 fallback = transform.position;
        Vector3 dir = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : transform.right;

        // Candidate A: manually placed muzzle transform (if present)
        Vector3 muzzleCandidate = muzzlePosition != null ? muzzlePosition.position : fallback;

        // Candidate B/C: both sprite endpoints along local X, then choose front-most by aim direction.
        Vector3 tipCandidate = fallback;
        if (gunRenderer != null && gunRenderer.sprite != null)
        {
            Bounds localBounds = gunRenderer.sprite.bounds;
            Vector3 localTipA = new Vector3(localBounds.max.x, 0f, 0f);
            Vector3 localTipB = new Vector3(localBounds.min.x, 0f, 0f);

            Vector3 worldTipA = gunRenderer.transform.TransformPoint(localTipA);
            Vector3 worldTipB = gunRenderer.transform.TransformPoint(localTipB);

            float dotA = Vector3.Dot(worldTipA - fallback, dir);
            float dotB = Vector3.Dot(worldTipB - fallback, dir);
            tipCandidate = dotA >= dotB ? worldTipA : worldTipB;
        }

        // Always pick the point farther in the shooting direction to avoid spawning from stock.
        float muzzleDot = Vector3.Dot(muzzleCandidate - fallback, dir);
        float tipDot = Vector3.Dot(tipCandidate - fallback, dir);
        return tipDot > muzzleDot ? tipCandidate : muzzleCandidate;
    }

    void SpawnBullet(Vector3 pos, Quaternion baseRot, float angleOffset)
    {
        Quaternion rot = baseRot * Quaternion.Euler(0, 0, angleOffset);
        GameObject bulletObj = Instantiate(gunData.bulletPrefab, pos, rot);

        if (gunData.bulletSprite != null)
        {
            SpriteRenderer bulletRenderer = bulletObj.GetComponentInChildren<SpriteRenderer>();
            if (bulletRenderer != null)
                bulletRenderer.sprite = gunData.bulletSprite;
        }

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2 bulletDir = (rot * Vector3.right).normalized;
            bullet.direction = bulletDir;
            bullet.damage = Mathf.RoundToInt(gunData.damage * damageMultiplier);
            bullet.speed  = gunData.bulletSpeed;
            bullet.lifeTime = gunData.bulletLifeTime;
        }
    }

    IEnumerator ShowMuzzleFlash()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        muzzleFlash.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (gunData == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gunData.detectRange);
    }
}