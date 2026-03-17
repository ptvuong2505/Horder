using UnityEngine;

/// <summary>
/// ParallaxLayer
/// Gắn vào mỗi GameObject con trong hệ thống Parallax.
/// Di chuyển theo camera với hệ số parallaxSpeed.
///   parallaxSpeed = 0   → layer tĩnh hoàn toàn (background xa nhất)
///   parallaxSpeed = 1   → layer đi cùng tốc độ camera (không có hiệu ứng parallax)
///   0 < parallaxSpeed < 1 → vùng parallax hiệu quả
/// 
/// Hỗ trợ infinite scroll theo trục X (và tùy chọn trục Y).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────────────────
    [Header("Parallax Settings")]
    [Tooltip("0 = tĩnh hoàn toàn (xa nhất), 1 = đi cùng camera (không hiệu ứng)")]
    [Range(0f, 1f)]
    public float parallaxSpeedX = 0.1f;

    [Tooltip("Parallax theo trục Y. Thường để 0 cho game 2D side-scroll.")]
    [Range(0f, 1f)]
    public float parallaxSpeedY = 0f;

    [Tooltip("Bật infinite scroll theo trục X")]
    public bool infiniteScrollX = true;

    [Tooltip("Bật infinite scroll theo trục Y (hiếm dùng)")]
    public bool infiniteScrollY = false;

    // ────────────────────────────────────────────────────────────
    //  Private
    // ────────────────────────────────────────────────────────────
    private float _spriteWidth;
    private float _spriteHeight;
    private float _startPosX;
    private float _startPosY;

    // ────────────────────────────────────────────────────────────

    void Start()
    {
        _startPosX = transform.position.x;
        _startPosY = transform.position.y;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            _spriteWidth  = sr.bounds.size.x;
            _spriteHeight = sr.bounds.size.y;
        }
    }

    /// <summary>
    /// Gọi mỗi frame từ ParallaxBackground, truyền vào camera delta (độ thay đổi vị trí camera).
    /// </summary>
    public void UpdateLayer(Vector2 cameraDelta)
    {
        // Di chuyển layer theo parallax
        float newX = transform.position.x + cameraDelta.x * parallaxSpeedX;
        float newY = transform.position.y + cameraDelta.y * parallaxSpeedY;

        transform.position = new Vector3(newX, newY, transform.position.z);

        // Infinite scroll X
        if (infiniteScrollX && _spriteWidth > 0f)
        {
            // Tính khoảng cách từ vị trí ban đầu (theo không gian camera)
            float distX = transform.position.x - _startPosX;
            if (Mathf.Abs(distX) >= _spriteWidth)
            {
                float offset = distX > 0 ? -_spriteWidth : _spriteWidth;
                transform.position = new Vector3(transform.position.x + offset,
                                                  transform.position.y,
                                                  transform.position.z);
                _startPosX = transform.position.x - (distX + offset); // reset anchor
            }
        }

        // Infinite scroll Y
        if (infiniteScrollY && _spriteHeight > 0f)
        {
            float distY = transform.position.y - _startPosY;
            if (Mathf.Abs(distY) >= _spriteHeight)
            {
                float offset = distY > 0 ? -_spriteHeight : _spriteHeight;
                transform.position = new Vector3(transform.position.x,
                                                  transform.position.y + offset,
                                                  transform.position.z);
                _startPosY = transform.position.y - (distY + offset);
            }
        }
    }
}
