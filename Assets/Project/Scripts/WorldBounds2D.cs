using UnityEngine;

/// <summary>
/// WorldBounds2D
/// Tạo "rào chắn" vô hình để Player (và/hoặc enemy) không chạy ra ngoài background.
/// 
/// Cơ chế:
/// - Script sẽ tạo 4 BoxCollider2D (trên/ dưới/ trái/ phải) dạng static.
/// - Các collider này dùng để chặn Rigidbody2D.
/// 
/// Cách dùng nhanh:
/// 1) Tạo 1 GameObject rỗng tên "WorldBounds".
/// 2) Add component WorldBounds2D.
/// 3) Chỉnh boundSize theo kích thước map/background (world units).
/// 4) Đảm bảo Player có Rigidbody2D + Collider2D.
/// 
/// Tip đo size:
/// - Nếu background là SpriteRenderer: bạn có thể ước lượng size theo sprite.bounds.size.
/// - Nếu dùng Tilemap: lấy bounds của tilemap.
/// </summary>
public class WorldBounds2D : MonoBehaviour
{
    [Header("Bounds")]
    [Tooltip("Kích thước khu vực cho phép (world units).")]
    public Vector2 boundSize = new Vector2(40f, 22f);

    [Tooltip("Tâm của bounds (world position).")]
    public Vector2 boundCenter = Vector2.zero;

    [Tooltip("Độ dày tường collider.")]
    public float wallThickness = 1f;

    [Header("Collision")]
    [Tooltip("Layer cho walls. Tuỳ chọn - có thể để Default.")]
    public int wallLayer = 0;

    [Tooltip("Nếu true, sẽ tự rebuild walls khi Start.")]
    public bool buildOnStart = true;

    private BoxCollider2D left;
    private BoxCollider2D right;
    private BoxCollider2D top;
    private BoxCollider2D bottom;

    private void Start()
    {
        if (buildOnStart)
            Rebuild();
    }

    [ContextMenu("Rebuild Bounds")]
    public void Rebuild()
    {
        CreateOrGet(ref left, "Wall_Left");
        CreateOrGet(ref right, "Wall_Right");
        CreateOrGet(ref top, "Wall_Top");
        CreateOrGet(ref bottom, "Wall_Bottom");

        ApplyWallTransforms();
    }

    private void CreateOrGet(ref BoxCollider2D col, string name)
    {
        Transform t = transform.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            t = go.transform;
        }

        t.gameObject.layer = wallLayer;

        col = t.GetComponent<BoxCollider2D>();
        if (col == null) col = t.gameObject.AddComponent<BoxCollider2D>();

        col.isTrigger = false;

        // Để chắc chắn collider có Rigidbody2D static (Unity 2D physics ổn định hơn)
        Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
        if (rb == null) rb = t.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;
    }

    private void ApplyWallTransforms()
    {
        float halfW = boundSize.x * 0.5f;
        float halfH = boundSize.y * 0.5f;

        // Vị trí 4 mép
        Vector2 leftPos = boundCenter + new Vector2(-halfW - wallThickness * 0.5f, 0f);
        Vector2 rightPos = boundCenter + new Vector2(halfW + wallThickness * 0.5f, 0f);
        Vector2 topPos = boundCenter + new Vector2(0f, halfH + wallThickness * 0.5f);
        Vector2 bottomPos = boundCenter + new Vector2(0f, -halfH - wallThickness * 0.5f);

        // Size collider
        Vector2 verticalSize = new Vector2(wallThickness, boundSize.y + wallThickness * 2f);
        Vector2 horizontalSize = new Vector2(boundSize.x + wallThickness * 2f, wallThickness);

        SetWall(left, leftPos, verticalSize);
        SetWall(right, rightPos, verticalSize);
        SetWall(top, topPos, horizontalSize);
        SetWall(bottom, bottomPos, horizontalSize);
    }

    private void SetWall(BoxCollider2D col, Vector2 worldPos, Vector2 size)
    {
        if (col == null) return;
        col.size = size;
        col.offset = Vector2.zero;
        col.transform.position = worldPos;
        col.transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boundCenter, new Vector3(boundSize.x, boundSize.y, 0f));
    }
}
