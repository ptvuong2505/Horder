using UnityEngine;

/// <summary>
/// EnemyProjectile – Đạn do enemy bắn ra, gây damage cho Player.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifeTime = 3f;
    public int damage = 6;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.right * speed * Time.deltaTime;
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
