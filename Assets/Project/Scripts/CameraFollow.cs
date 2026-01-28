using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // Player
    public float smoothSpeed = 5f; // Độ mượt camera
    public Vector3 offset;         // Khoảng cách camera

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = transform.position.z;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
