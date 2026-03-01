using TMPro;
using UnityEngine;

/// <summary>
/// ScoreUI – Hiển thị điểm số realtime.
/// Gắn vào Canvas > ScoreUI object.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI scoreText;   // Text hiển thị điểm

    [Header("Animation")]
    public bool punchOnScore = true;    // Scale nhanh khi cộng điểm
    private Vector3 originalScale;
    private float punchTimer = 0f;
    private float punchDuration = 0.15f;

    void Start()
    {
        originalScale = transform.localScale;

        UpdateScoreText(0);

        if (GameManager.Instance != null)
            GameManager.Instance.OnScoreChanged += UpdateScoreText;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnScoreChanged -= UpdateScoreText;
    }

    void Update()
    {
        if (punchTimer > 0f)
        {
            punchTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(punchTimer / punchDuration);
            // Scale nhanh lên rồi về cũ
            float scale = Mathf.Lerp(1.3f, 1f, t);
            transform.localScale = originalScale * scale;
        }
    }

    void UpdateScoreText(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore}";

        if (punchOnScore)
            punchTimer = punchDuration;
    }
}
