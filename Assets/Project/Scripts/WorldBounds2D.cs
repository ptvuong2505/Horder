using UnityEngine;

/// <summary>
/// WorldBounds2D
/// Tạo "rào chắn" vô hình để Player (và/hoặc enemy) không chạy ra ngoài background.
/// 
/// Ý tưởng chính:
/// - Bạn khai báo 1 hình chữ nhật (boundCenter + boundSize) là khu vực được phép di chuyển.
/// - Script sẽ tạo 4 BoxCollider2D bao quanh hình chữ nhật đó (trái/phải/trên/dưới).
/// - Khi Player có Rigidbody2D + Collider2D va chạm các collider này, Player sẽ bị chặn lại.
/// </summary>
public class WorldBounds2D : MonoBehaviour
{
    // =============================
    // 1) DỮ LIỆU BOUNDS (KHU VỰC ĐƯỢC PHÉP ĐI)
    // =============================

    [Header("Bounds")]
    [Tooltip("Kích thước khu vực cho phép (world units). Ví dụ: (40,22) nghĩa là rộng 40, cao 22.")]
    public Vector2 boundSize = new Vector2(40f, 22f);

    [Tooltip("Tâm của bounds (world position). Nếu background của bạn nằm giữa map thì thường là (0,0).")]
    public Vector2 boundCenter = Vector2.zero;

    [Tooltip("Độ dày tường collider. Tường càng dày càng khó bị lọt qua khi tốc độ cao.")]
    public float wallThickness = 1f;

    // =============================
    // 2) CẤU HÌNH VA CHẠM
    // =============================

    [Header("Collision")]
    [Tooltip("Layer cho walls. Dùng để tuỳ chỉnh Physics2D collision matrix (chặn Player nhưng không chặn thứ khác...).")]
    public int wallLayer = 0;

    [Tooltip("Nếu true: khi Start() sẽ tự gọi Rebuild() để tạo/đặt lại 4 tường.")]
    public bool buildOnStart = true;

    // 4 collider đại diện cho 4 mặt tường.
    private BoxCollider2D left;
    private BoxCollider2D right;
    private BoxCollider2D top;
    private BoxCollider2D bottom;

    private void Start()
    {
        // Khi vào Play Mode, nếu buildOnStart bật thì tự tạo tường.
        if (buildOnStart)
            Rebuild();
    }

    /// <summary>
    /// Rebuild Bounds
    /// - Đảm bảo 4 "tường" tồn tại (tạo mới nếu chưa có).
    /// - Sau đó tính toán vị trí/kích thước của từng tường và apply lên collider.
    /// 
    /// Bạn có thể gọi hàm này thủ công trong Inspector bằng context menu.
    /// </summary>
    [ContextMenu("Rebuild Bounds")]
    public void Rebuild()
    {
        // Tạo (hoặc lấy lại) 4 GameObject con chứa BoxCollider2D.
        CreateOrGet(ref left, "Wall_Left");
        CreateOrGet(ref right, "Wall_Right");
        CreateOrGet(ref top, "Wall_Top");
        CreateOrGet(ref bottom, "Wall_Bottom");

        // Sau khi đã có đủ collider, đặt vị trí + set size dựa trên boundSize/boundCenter.
        ApplyWallTransforms();
    }

    /// <summary>
    /// CreateOrGet
    /// - Tìm GameObject con theo tên (Wall_Left/Right/Top/Bottom).
    /// - Nếu chưa có thì tạo mới.
    /// - Đảm bảo object có BoxCollider2D (isTrigger=false) và Rigidbody2D Static.
    /// </summary>
    private void CreateOrGet(ref BoxCollider2D col, string name)
    {
        // 1) Tìm object con trong hierarchy của WorldBounds
        Transform t = transform.Find(name);

        // 2) Nếu chưa có -> tạo mới
        if (t == null)
        {
            GameObject go = new GameObject(name);

            // SetParent(..., false) để giữ local transform mặc định, giúp dễ quản lý.
            go.transform.SetParent(transform, false);
            t = go.transform;
        }

        // 3) Set layer để bạn dùng Physics2D collision matrix kiểm soát va chạm.
        t.gameObject.layer = wallLayer;

        // 4) Đảm bảo có BoxCollider2D
        col = t.GetComponent<BoxCollider2D>();
        if (col == null) col = t.gameObject.AddComponent<BoxCollider2D>();

        // isTrigger=false => đây là "tường thật" để chặn Rigidbody2D (không xuyên qua)
        col.isTrigger = false;

        // 5) Đảm bảo có Rigidbody2D Static
        // Unity 2D physics thường ổn định hơn khi collider tĩnh có Rigidbody2D (bodyType Static).
        Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
        if (rb == null) rb = t.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;
    }

    /// <summary>
    /// ApplyWallTransforms
    /// Tính toán vị trí + kích thước của 4 collider tường.
    /// 
    /// boundSize = (W,H): vùng hợp lệ là 1 hình chữ nhật.
    /// - Tường trái/phải: collider dọc (vertical)
    /// - Tường trên/dưới: collider ngang (horizontal)
    /// 
    /// wallThickness được dùng để đẩy tường ra ngoài mép "vùng hợp lệ" một chút.
    /// </summary>
    private void ApplyWallTransforms()
    {
        // halfW/halfH là nửa kích thước vùng hợp lệ.
        float halfW = boundSize.x * 0.5f;
        float halfH = boundSize.y * 0.5f;

        // 1) Tính vị trí tâm của từng tường (world position)
        // Lưu ý: -halfW là mép trái, +halfW là mép phải, tương tự cho Y.
        // Cộng thêm wallThickness*0.5f để tường nằm "bên ngoài" vùng hợp lệ.
        Vector2 leftPos = boundCenter + new Vector2(-halfW - wallThickness * 0.5f, 0f);
        Vector2 rightPos = boundCenter + new Vector2(halfW + wallThickness * 0.5f, 0f);
        Vector2 topPos = boundCenter + new Vector2(0f, halfH + wallThickness * 0.5f);
        Vector2 bottomPos = boundCenter + new Vector2(0f, -halfH - wallThickness * 0.5f);

        // 2) Tính kích thước collider
        // - verticalSize: tường trái/phải (mỏng theo X, dài theo Y)
        // - horizontalSize: tường trên/dưới (dài theo X, mỏng theo Y)
        Vector2 verticalSize = new Vector2(wallThickness, boundSize.y + wallThickness * 2f);
        Vector2 horizontalSize = new Vector2(boundSize.x + wallThickness * 2f, wallThickness);

        // 3) Áp dụng vào từng collider
        SetWall(left, leftPos, verticalSize);
        SetWall(right, rightPos, verticalSize);
        SetWall(top, topPos, horizontalSize);
        SetWall(bottom, bottomPos, horizontalSize);
    }

    /// <summary>
    /// SetWall
    /// Áp vị trí/size vào collider tường.
    /// </summary>
    private void SetWall(BoxCollider2D col, Vector2 worldPos, Vector2 size)
    {
        if (col == null) return;

        col.size = size;          // kích thước collider
        col.offset = Vector2.zero; // offset = 0 để collider nằm đúng tâm object

        // Đặt world position trực tiếp (tường nằm đúng vị trí tính toán)
        col.transform.position = worldPos;
        col.transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ gizmo để bạn nhìn thấy khung bounds trong Scene view (khi chọn object WorldBounds).
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boundCenter, new Vector3(boundSize.x, boundSize.y, 0f));
    }
}
