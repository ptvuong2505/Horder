using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DashController2D
/// Thêm cơ chế Dash cho Player (survivor-like / top-down 2D).
/// 
/// Cách hoạt động:
/// - Nhấn Dash (gợi ý bind: Shift hoặc Space) để lao nhanh theo hướng đang di chuyển.
/// - Trong thời gian dash: set Rigidbody2D velocity mạnh, có thể bật "invulnerable".
/// - Có cooldown + (tuỳ chọn) stamina/charges.
/// 
/// Cách gắn:
/// - Add component này lên Player cùng với Rigidbody2D.
/// - Với Input System: tạo action "Dash" (Button) rồi trỏ event về hàm OnDash(...).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DashController2D : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Tốc độ dash (đơn vị/giây).")]
    public float dashSpeed = 20f;

    [Tooltip("Thời gian dash (giây).")]
    public float dashDuration = 0.12f;

    [Tooltip("Cooldown giữa 2 lần dash (giây).")]
    public float dashCooldown = 0.8f;

    [Header("I-Frames (Optional)")]
    [Tooltip("Nếu true, Player sẽ không nhận damage trong lúc dash.")]
    public bool invulnerableDuringDash = true;

    [Header("Stamina")]
    [Tooltip("Stamina tối đa. Dash sẽ tiêu tốn staminaDashCost mỗi lần.")]
    public float staminaMax = 100f;

    [Tooltip("Stamina hiện tại (runtime).")]
    public float stamina = 100f;

    [Tooltip("Stamina tiêu tốn mỗi lần dash.")]
    public float staminaDashCost = 35f;

    [Tooltip("Tốc độ hồi stamina mỗi giây.")]
    public float staminaRegenPerSecond = 30f;

    [Tooltip("Trễ hồi stamina sau khi dash (giây).")]
    public float staminaRegenDelayAfterDash = 0.25f;

    private float lastDashTime;

    /// <summary>
    /// Stamina (0..1) để UI hiển thị thanh.
    /// </summary>
    public float StaminaNormalized => staminaMax <= 0 ? 0f : Mathf.Clamp01(stamina / staminaMax);

    // State
    private Rigidbody2D rb;
    private Player player;
    private bool dashQueued;
    private bool isDashing;
    private float nextDashTime;

    // Hướng dash: ưu tiên hướng move hiện tại; nếu đứng yên thì dash theo hướng nhìn (fallback).
    private Vector2 lastMoveDir = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        // Ensure stamina khởi tạo đúng.
        staminaMax = Mathf.Max(1f, staminaMax);
        stamina = Mathf.Clamp(stamina, 0f, staminaMax);
    }

    private void Update()
    {
        // Regen stamina (không regen ngay lập tức sau dash).
        if (!isDashing && Time.time >= lastDashTime + staminaRegenDelayAfterDash)
        {
            stamina = Mathf.Min(staminaMax, stamina + staminaRegenPerSecond * Time.deltaTime);
        }

        // Lấy hướng di chuyển từ Rigidbody velocity.
        // (Cách này không phụ thuộc vào việc Player lưu moveInput là private.)
        Vector2 v = rb != null ? rb.linearVelocity : Vector2.zero;
        if (v.sqrMagnitude > 0.01f)
            lastMoveDir = v.normalized;

        if (dashQueued)
        {
            dashQueued = false;
            TryStartDash();
        }
    }

    /// <summary>
    /// Input System callback. Bind vào action "Dash" (Button).
    /// </summary>
    public void OnDash(InputAction.CallbackContext context)
    {
        // performed = lúc nhấn.
        if (context.performed)
            dashQueued = true;
    }

    /// <summary>
    /// Input System callback (phím Shift hiện tại trong project đang map vào action "Sprint").
    /// Gắn event Sprint -> gọi hàm này để dash.
    /// </summary>
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
            dashQueued = true;
    }

    private void TryStartDash()
    {
        if (isDashing) return;

        // Giữ cooldown như một "anti-spam" nhỏ, nhưng stamina mới là giới hạn chính.
        if (Time.time < nextDashTime) return;

        // Nếu player đã chết thì không dash.
        // (Player.dead là private, nên check bằng HP hoặc state khác; ở đây dùng currentHealth > 0.)
        if (player != null && player.currentHealth <= 0) return;

        // Không đủ stamina thì không dash.
        if (stamina < staminaDashCost) return;

        stamina -= staminaDashCost;
        StartCoroutine(DashRoutine(lastMoveDir));
    }

    private IEnumerator DashRoutine(Vector2 dir)
    {
        isDashing = true;
        lastDashTime = Time.time;
        nextDashTime = Time.time + dashCooldown;

        Vector2 preDashVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

        if (invulnerableDuringDash && player != null)
            player.invulnerable = true;

        float t = 0f;
        while (t < dashDuration)
        {
            t += Time.deltaTime;
            if (rb != null)
                rb.linearVelocity = dir * dashSpeed;
            yield return null;
        }

        if (invulnerableDuringDash && player != null)
            player.invulnerable = false;

        // Trả lại một phần velocity trước đó để cảm giác không bị "khựng".
        if (rb != null)
            rb.linearVelocity = preDashVelocity;

        isDashing = false;
    }

    public bool IsDashing => isDashing;
    public float CooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
}
