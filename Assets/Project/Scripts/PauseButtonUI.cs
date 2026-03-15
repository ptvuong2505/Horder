using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PauseButtonUI – Gắn vào nút Pause trong HUD.
/// Dùng Input System detect click trực tiếp, không cần EventSystem trong scene.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PauseButtonUI : MonoBehaviour
{
    private RectTransform rect;
    private Canvas parentCanvas;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // ScreenSpace Overlay dùng null camera
        Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera
            : null;

        if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, cam))
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.PauseGame();
        }
    }
}

