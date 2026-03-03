using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 2f;

    // damage có thể set khác nhau cho từng loại súng / prefab
    public int damage = 10;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.right * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Gây damage cho Enemy nếu có component Enemy
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Hit(damage);
            }

            // Gây damage cho BossEnemy nếu có component BossEnemy
            BossEnemy boss = collision.GetComponent<BossEnemy>();
            if (boss != null)
            {
                boss.Hit(damage);
            }

            Destroy(gameObject);
        }
    }
}
