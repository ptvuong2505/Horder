using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth;
    public float speed;
    public int damageToPlayer = 10;

    private int currentHealth;
    Animator anim;
    Rigidbody2D rb;

    Transform target;
    
    private float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
        
        // Đăng ký với EnemyManager
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(gameObject);
        }
    }

    void Update()
    {
        if (target != null && rb != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * speed, direction.y * speed);
            Flip(direction);
        }
    }

    public void Hit(int damage)
    {
        currentHealth -= damage;
        if (anim != null)
            anim.SetTrigger("Hit");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Flip(Vector3 dir)
    {
        if (dir.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(-dir.x),
                1,
                1
            );
        }
    }

    void Die()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(gameObject);
        }
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AttackPlayer(collision.gameObject);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer(collision.gameObject);
            }
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