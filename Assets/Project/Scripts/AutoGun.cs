using UnityEngine;

public class AutoGun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform muzzlePosition;
    public float fireRate = 0.5f;
    public float detectRange = 10f;

    float fireTimer;

    void Update()
    {
        GameObject target = FindNearestEnemy();

        if (target == null) return;

        RotateToTarget(target.transform);

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance && distance <= detectRange)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    void RotateToTarget(Transform target)
    {
        Vector2 direction = target.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Shoot()
    {
        Instantiate(
            bulletPrefab,
            muzzlePosition.position,
            transform.rotation
        );
    }
}
