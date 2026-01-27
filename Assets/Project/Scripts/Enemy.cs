using UnityEngine;

public class Enemy : MonoBehaviour
{

    public int maxHealth;
    public float speed;

    private int currentHealth;
    Animator anim;

    // lấy vị trí người chơi
    Transform target;
    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Player").transform;

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        Flip(direction);

    }

    public void Hit(int damage)
    {
        currentHealth -= damage;
        anim.SetTrigger("Hit");
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
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
}
