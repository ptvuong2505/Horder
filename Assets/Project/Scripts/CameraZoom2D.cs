using UnityEngine;

/// <summary>
/// CameraZoom2D
/// Script tiện để chỉnh "tầm nhìn" trong game 2D orthographic.
/// 
/// Cách dùng:
/// - Gắn lên Main Camera.
/// - Với camera Orthographic: giá trị orthographicSize CÀNG LỚN -> nhìn CÀNG RỘNG.
/// - Có thể chỉnh trực tiếp orthographicSize trong Inspector mà không cần script,
///   nhưng script này giúp bạn có clamp + chỉnh runtime nếu cần.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoom2D : MonoBehaviour
{
    [Tooltip("Orthographic size mong muốn. Lớn hơn = nhìn rộng hơn.")]
    public float orthographicSize = 6.5f;

    [Tooltip("Nếu true, mỗi frame sẽ ép camera về orthographicSize.")]
    public bool applyContinuously = true;

    [Tooltip("Clamp nhỏ nhất.")]
    public float minSize = 3f;

    [Tooltip("Clamp lớn nhất.")]
    public float maxSize = 12f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (applyContinuously)
            Apply();
    }

    public void Apply()
    {
        if (cam == null) return;
        if (!cam.orthographic) return;

        cam.orthographicSize = Mathf.Clamp(orthographicSize, minSize, maxSize);
    }
}
