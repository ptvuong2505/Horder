using System.Collections;
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

    private float fireCooldown = 0f;
    private Transform currentTarget;

    // Multipliers cho upgrade/pickup (mặc định = 1)
    [HideInInspector] public float damageMultiplier = 1f;
    [HideInInspector] public float fireRateMultiplier = 1f;    // Gọi từ WeaponManager để truyền GunData vào
    public void Setup(GunData data)
    {
        gunData = data;
        fireCooldown = 0f;
        Debug.Log($"[AutoGun] {gameObject.name} đã nhận GunData: {data?.gunName}");
    }

    void Start()
    {
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    void Update()
    {
        if (gunData == null) return;

        fireCooldown -= Time.deltaTime;

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
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Fire()
    {
        if (gunData.bulletPrefab == null)
        {
            Debug.LogWarning($"[AutoGun] {gameObject.name}: bulletPrefab chưa gán trong GunData!");
            return;
        }

        Vector3 spawnPos = muzzlePosition != null ? muzzlePosition.position : transform.position;
        Quaternion baseRot = muzzlePosition != null ? muzzlePosition.rotation : transform.rotation;

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

    void SpawnBullet(Vector3 pos, Quaternion baseRot, float angleOffset)
    {
        Quaternion rot = baseRot * Quaternion.Euler(0, 0, angleOffset);
        GameObject bulletObj = Instantiate(gunData.bulletPrefab, pos, rot);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = Mathf.RoundToInt(gunData.damage * damageMultiplier);
            bullet.speed  = gunData.bulletSpeed;
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