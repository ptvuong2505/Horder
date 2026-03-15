using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Vector2 moveInput;
    public float speed = 10f;
    Animator animator;
    Rigidbody2D rb;

    bool dead;
    public int maxHealth = 150;
    public int currentHealth;

    // Event khi HP thay đổi (HealthBar sẽ lắng nghe)
    public event Action<int, int> OnHealthChanged; // (currentHP, maxHP)

    // Event khi Player chết
    public event System.Action OnDeath;

    // ──────────────────────────────────────────────
    //  Public methods cho Upgrade / Pickup
    // ──────────────────────────────────────────────
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddMaxHP(int amount)
    {
        maxHealth += amount;
        currentHealth += amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddSpeed(float amount)
    {
        speed += amount;
    }

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
        if (context.performed || context.canceled)
        {
            moveInput = context.ReadValue<Vector2>();
            animator.SetFloat("velocity", moveInput.magnitude);

        }
    }
    private void Update()
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

        // SFX player bị đánh
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerHit();

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

        // Báo GameManager xử lý Game Over
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
        else
        {
            // Fallback nếu không có GameManager
            Invoke(nameof(LoadGameOverScene), 2f);
        }
    }

    void LoadGameOverScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("bg_game_over");
    }
}
