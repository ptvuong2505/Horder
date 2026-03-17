using UnityEngine;

/// <summary>
/// ParallaxBackground
/// 
/// Manager cho hệ thống Parallax nhiều lớp.
/// Gắn script này lên GameObject cha chứa tất cả các layer.
/// 
/// Mỗi GameObject con cần có:
///   - SpriteRenderer (với sprite đã set)
///   - ParallaxLayer (component xử lý từng layer)
/// 
/// Cách setup trong Inspector:
///   1. Tạo GameObject "ParallaxBackground" trong scene
///   2. Gắn script này vào
///   3. Tạo các GameObject con, mỗi con = 1 layer
///   4. Đặt SpriteRenderer + ParallaxLayer trên mỗi con
///   5. Chỉnh parallaxSpeedX trên từng ParallaxLayer
///      (0 = tĩnh/xa nhất, ~0.4 = gần nhất)
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────
    //  Inspector
    // ────────────────────────────────────────────────────────────
    [Header("Camera Reference")]
    [Tooltip("Để trống → tự động tìm Camera.main khi Start()")]
    public Camera targetCamera;

    [Header("Global Multiplier")]
    [Tooltip("Nhân thêm vào tốc độ của tất cả các layer (giá trị mặc định 1 = theo từng layer)")]
    public float globalSpeedMultiplier = 1f;

    // ────────────────────────────────────────────────────────────
    //  Private
    // ────────────────────────────────────────────────────────────
    private ParallaxLayer[] _layers;
    private Vector3 _previousCamPos;

    // ────────────────────────────────────────────────────────────

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError("[ParallaxBackground] Không tìm thấy Camera. Hãy gán targetCamera trong Inspector.");
            enabled = false;
            return;
        }

        _previousCamPos = targetCamera.transform.position;

        // Thu thập tất cả ParallaxLayer trong các con
        _layers = GetComponentsInChildren<ParallaxLayer>(includeInactive: true);

        if (_layers.Length == 0)
            Debug.LogWarning("[ParallaxBackground] Không có ParallaxLayer nào trong các GameObject con.");
    }

    void LateUpdate()
    {
        if (targetCamera == null || _layers == null) return;

        Vector3 currentCamPos = targetCamera.transform.position;
        Vector2 delta = new Vector2(
            (currentCamPos.x - _previousCamPos.x) * globalSpeedMultiplier,
            (currentCamPos.y - _previousCamPos.y) * globalSpeedMultiplier
        );

        // Cập nhật từng layer
        foreach (ParallaxLayer layer in _layers)
        {
            if (layer != null && layer.gameObject.activeInHierarchy)
                layer.UpdateLayer(delta);
        }

        _previousCamPos = currentCamPos;
    }

#if UNITY_EDITOR
    // Vẽ gizmo để dễ nhận biết trong Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(5f, 3f, 0f));

        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.7f,
            "[ParallaxBackground]");
    }
#endif
}
