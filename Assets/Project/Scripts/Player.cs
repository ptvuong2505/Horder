using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player :MonoBehaviour
{
    private Vector2 moveInput;
    public float speed = 10f;
    Animator animator;
    Rigidbody2D rb;

    bool dead;
    int maxHealth = 100;
    int currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if(context.performed || context.canceled)
        {
            moveInput = context.ReadValue<Vector2>();
            animator.SetFloat("velocity", moveInput.magnitude);
            
        }
    }

    private void Update()
    {
        if (dead)
        {
            moveInput = Vector2.zero;
        }

        if (moveInput.x != 0)
        {
            var facingDirection = moveInput.x > 0 ? 1 : -1;
            transform.localScale = new Vector2(facingDirection, 1);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * speed;
    }

    void Hit(int damage)
    {
        animator.SetTrigger("hit");
        currentHealth -= damage;
        if (currentHealth <= 0 && !dead)
        {
            Die();
            //animator.SetTrigger("die");
        }
    }

    void Die()
    {
        dead = true;
    }
}
