using Assets.Project.Scripts;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player
/// Chịu trách nhiệm:
/// - Nhận input di chuyển (Input System) và set velocity cho Rigidbody2D.
/// - Quản lý HP (Heal/Hit/AddMaxHP) và phát event OnHealthChanged cho UI.
/// - Khi chết: phát OnDeath và báo GameManager.TriggerGameOver().
/// 
/// Ghi chú:
/// - Gameplay hiện tại thiên về "survivor-like" (bắn tự động), nên Player thường cần thêm:
///   Dash/Skill/Interact... (chưa implement).
/// </summary>
public class Player : MonoBehaviour
{
    private Vector2 moveInput;
    public float speed = 10f;
    Animator animator;
    Rigidbody2D rb;

    [SerializeField] private Transform spriteRoot; // chính là object "Sprite"
    public SpriteRenderer footR;
    public SpriteRenderer footL;
    public SpriteRenderer body;

    bool dead;
    public int maxHealth = 150;
    public int currentHealth;

    [Header("Combat")]
    [Tooltip("Nếu true, Player tạm thời không nhận damage (dùng cho dash i-frames, skill shield...).")]
    public bool invulnerable = false;

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

    public void Initialize(PlayerData data)
    {
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;
        speed = data.speed;

        // xóa face cũ
        Transform face = body.transform.Find("Face");
        if (face != null)
            Destroy(face.gameObject);

        body.sprite = data.playerPrefab.body.sprite;
        footL.sprite = data.playerPrefab.footL.sprite;
        footR.sprite = data.playerPrefab.footR.sprite;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Fallback an toàn: nếu chưa được Initialize từ PlayerData,
        // đảm bảo player luôn bắt đầu với HP hợp lệ.
        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    void Start()
    {
        // Guard lần cuối trong trường hợp thứ tự khởi tạo khiến HP bị 0.
        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;

        // Gửi HP ban đầu cho HealthBar
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // Input System gọi function này qua PlayerInput.
        // performed/canceled để lấy cả lúc nhả phím (vector = 0).
        if (context.performed || context.canceled)
        {
            moveInput = context.ReadValue<Vector2>();
            animator.SetFloat("velocity", moveInput.magnitude);

        }
    }

    private void Update()
    {
        // Movement được điều khiển trực tiếp bằng velocity.
        // Khi dead = true, khóa movement.
        if (!dead)
        {
            rb.linearVelocity = moveInput * speed;
            if (moveInput.x != 0)
            {
                Vector3 scale = spriteRoot.localScale;
                scale.x = Mathf.Sign(moveInput.x) * Mathf.Abs(scale.x);
                spriteRoot.localScale = scale;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // PUBLIC - Enemy có thể gọi để gây sát thương
    public void Hit(int damage)
    {
        // Nếu đang có i-frames (dash/skill), bỏ qua damage.
        if (invulnerable) return;

        if (dead)
            return; // Đã chết thì không nhận damage nữa

        animator.SetTrigger("hit");
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

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
