using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StaminaBarUI
/// Thanh "stamina" trước đây, hiện được tái sử dụng để hiển thị trạng thái Dash.
/// 
/// Lý do:
/// - DashController2D đã bỏ cơ chế stamina.
/// - UI này đổi sang hiển thị "cooldown của dash" (càng đầy nghĩa là càng sẵn sàng dash).
/// 
/// Cách dùng:
/// - Tạo 1 UI Image (Fill) dạng thanh ngang nhỏ.
/// - Gán vào field fillImage.
/// - Gán player (hoặc để trống, script sẽ tự tìm theo tag Player).
/// - Script sẽ lấy DashController2D trên Player và cập nhật fill theo cooldown.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Player player;

    public enum RenderMode
    {
        /// <summary>
        /// Dùng Image.fillAmount (chỉ hoạt động khi Image Type = Filled).
        /// </summary>
        FilledImage,

        /// <summary>
        /// Không cần Image Type.
        /// Thanh sẽ thay đổi chiều dài (width) theo stamina.
        /// </summary>
        ResizeWidth
    }

    [Header("Render Mode")]
    [Tooltip("Khuyến nghị dùng ResizeWidth nếu Inspector không có Image Type.")]
    [SerializeField] private RenderMode renderMode = RenderMode.ResizeWidth;

    [Header("ResizeWidth Settings")]
    [Tooltip("RectTransform của object fill. Nếu để trống sẽ tự lấy từ fillImage.")]
    [SerializeField] private RectTransform fillRect;

    [Tooltip("Chiều rộng đầy (pixel). Nếu để 0 sẽ tự lấy theo width hiện tại lúc Awake/OnEnable.")]
    [SerializeField] private float fullWidth = 0f;

    [Header("Optional")]
    [Tooltip("Nếu true, thanh sẽ ẩn khi stamina đầy.")]
    [SerializeField] private bool hideWhenFull = false;

    private DashController2D dash;

    private void Awake()
    {
        ResolveReferences();
        ResolveRectRefs();
        CacheFullWidthIfNeeded();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResolveRectRefs();
        CacheFullWidthIfNeeded();
        UpdateUI(1f);
    }

    private void Update()
    {
        if (dash == null)
        {
            ResolveReferences();
            return;
        }

        // Dash không còn stamina, nên thanh này hiển thị "mức sẵn sàng" dựa trên cooldown:
        // - CooldownRemaining = 0 => sẵn sàng dash => hiển thị 100%
        // - CooldownRemaining gần = dashCooldown => vừa dash => hiển thị thấp
        float denom = Mathf.Max(0.0001f, dash.dashCooldown);
        float normalizedReady = 1f - Mathf.Clamp01(dash.CooldownRemaining / denom);
        UpdateUI(normalizedReady);
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<Player>();
        }

        if (player != null)
            dash = player.GetComponent<DashController2D>();
    }

    private void ResolveRectRefs()
    {
        if (fillRect == null && fillImage != null)
            fillRect = fillImage.rectTransform;
    }

    private void CacheFullWidthIfNeeded()
    {
        if (renderMode != RenderMode.ResizeWidth) return;
        if (fillRect == null) return;

        // Lưu width ban đầu để coi như "100% stamina".
        if (fullWidth <= 0f)
            fullWidth = fillRect.sizeDelta.x;
    }

    private void UpdateUI(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (renderMode == RenderMode.FilledImage)
        {
            if (fillImage != null)
                fillImage.fillAmount = normalized;
        }
        else // ResizeWidth
        {
            if (fillRect != null)
            {
                Vector2 size = fillRect.sizeDelta;
                size.x = Mathf.Max(0f, fullWidth * normalized);
                fillRect.sizeDelta = size;
            }
        }

        if (hideWhenFull)
            gameObject.SetActive(normalized < 0.999f);
    }
}
