using TMPro;
using UnityEngine;

/// <summary>
/// ScoreUI
/// (Deprecated) Score system đã bị remove.
/// Script này được giữ lại để không làm vỡ scene/prefab cũ đang reference ScoreUI.
/// Bạn có thể xóa hẳn component này khỏi Canvas khi dọn project.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        if (scoreText != null)
            scoreText.text = string.Empty;
    }
}
