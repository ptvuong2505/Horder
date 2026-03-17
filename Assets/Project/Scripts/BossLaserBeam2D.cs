using UnityEngine;

/// <summary>
/// BossLaserBeam2D
/// "Laser" dạng chiếu thẳng (beam) trong một khoảng thời gian ngắn rồi tắt.
/// 
/// Cách dùng:
/// - Tạo prefab laser: SpriteRenderer (sprite dài/mỏng) + BoxCollider2D (isTrigger) + script này.
/// - Boss2Enemy sẽ Instantiate prefab này và gọi Initialize().
/// 
/// Lưu ý:
/// - Collider được đặt theo hướng local X (transform.right).
/// - Laser không bay; chỉ đứng yên, hiển thị một đường thẳng rồi tự hủy.
/// </summary>
public class BossLaserBeam2D : MonoBehaviour
{
    [Header("Hiển thị")]
    [Tooltip("Nếu không set, script sẽ tự lấy SpriteRenderer trên object.")]
    public SpriteRenderer spriteRenderer;

    [Header("Va chạm")]
    [Tooltip("Nếu không set, script sẽ tự lấy BoxCollider2D trên object.")]
    public BoxCollider2D boxCollider;

    [Header("Thông số")]
    public float duration = 0.22f;

    [Tooltip("Chiều dài laser theo đơn vị world.")]
    public float length = 14f;

    [Tooltip("Độ dày laser theo đơn vị world.")]
    public float thickness = 0.5f;

    [Tooltip("Offset bắt đầu tia (để không đè lên người bắn).")]
    public float startOffset = 0.6f;

    private int damage;

    public void Initialize(Vector2 dir, int dmg, float beamLength, float beamThickness, float beamDuration)
    {
        damage = dmg;
        length = beamLength;
        thickness = beamThickness;
        duration = beamDuration;

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        // Xoay theo hướng bắn
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, ang);

        ApplySize();

        Destroy(gameObject, duration);
    }

    private void ApplySize()
    {
        // Đặt vị trí sao cho laser bắt đầu từ trước họng súng
        transform.position += transform.right * startOffset;

        // Scale sprite (khuyến nghị sprite là 1x1 hoặc có Pixels Per Unit hợp lý)
        if (spriteRenderer != null)
        {
            // Dùng scale trực tiếp để đơn giản và tương thích mọi sprite.
            // Local X = chiều dài, Local Y = độ dày.
            Vector3 s = transform.localScale;
            s.x = length;
            s.y = thickness;
            transform.localScale = s;
        }

        // Set collider theo local space
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(1f, 1f);

            // Vì mình scale object, collider size để 1,1 cũng ok.
            // Nhưng để chắc chắn (nếu bạn không muốn scale object), bạn có thể comment phần scale trên
            // và dùng collider.size = new Vector2(length, thickness) + offset = ...
            boxCollider.offset = new Vector2(0.5f, 0f); // đẩy collider ra phía trước một chút theo local X
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player p = other.GetComponent<Player>();
        if (p != null)
            p.Hit(damage);
    }
}
