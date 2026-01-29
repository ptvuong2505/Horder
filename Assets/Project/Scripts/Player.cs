using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

public class Player :MonoBehaviour
{
    private Vector2 moveInput;
    public float speed = 10f;
    Animator animator;
    Rigidbody2D rb;

    bool dead;
    public int maxHealth = 100;
    public int currentHealth;

    // Event khi HP thay đổi (HealthBar sẽ lắng nghe)
    public event Action<int, int> OnHealthChanged; // (currentHP, maxHP)
    
    // Event khi Player chết
    public event Action OnDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Gửi HP ban đầu cho HealthBar
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
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
            return;
        }

        if (moveInput.x != 0)
        {
            var facingDirection = moveInput.x > 0 ? 1 : -1;
            transform.localScale = new Vector2(facingDirection, 1);
        }
    }

    private void FixedUpdate()
    {
        if (!dead)
        {
            rb.linearVelocity = moveInput * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // PUBLIC - Enemy có thể gọi để gây sát thương
    public void Hit(int damage)
    {
        if (dead)
            return; // Đã chết thì không nhận damage nữa

        animator.SetTrigger("hit");
        currentHealth -= damage;

        // Thông báo HP thay đổi
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        dead = true;
        animator.SetTrigger("die");
        
        // Thông báo Player đã chết
        OnDeath?.Invoke();

        // Chờ 2 giây rồi chuyển sang scene Game Over
        Invoke(nameof(LoadGameOverScene), 2f);
    }

    void LoadGameOverScene()
    {
        SceneManager.LoadScene("bg_game_over");
    }
}
