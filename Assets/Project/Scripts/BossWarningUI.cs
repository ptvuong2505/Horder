using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller hiển thị cảnh báo boss.
/// Gắn vào WarningPanel (có CanvasGroup).
/// Gọi BossWarningUI.Instance.ShowAndHide(2f) từ EnemySpawner.
/// </summary>
public class BossWarningUI : MonoBehaviour
{
    public static BossWarningUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("CanvasGroup trên WarningPanel")]
    public CanvasGroup canvasGroup;

    [Tooltip("Script BossWarningEffect (shockwave)")]
    public BossWarningEffect shockwave;

    [Header("Audio")]
    public AudioClip warningSFX;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Timing")]
    public float fadeDuration = 0.3f;

    private AudioSource _audio;

    // ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Tự lấy CanvasGroup nếu chưa gán
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Tự tạo AudioSource
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f; // 2D

        // Ẩn panel ngay lúc start
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(true);
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Hiển thị cảnh báo rồi tự tắt.
    /// Dùng yield return StartCoroutine(ShowAndHide(2f)) trong EnemySpawner.
    /// </summary>
    public IEnumerator ShowAndHide(float totalDuration = 2f)
    {
        float holdTime = Mathf.Max(0f, totalDuration - fadeDuration * 2f);

        // Phát âm thanh
        if (warningSFX != null && _audio != null)
            _audio.PlayOneShot(warningSFX, volume);

        // Bật shockwave
        if (shockwave != null)
            shockwave.PlayWarning(totalDuration);

        // Fade IN
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // Giữ nguyên
        yield return new WaitForSecondsRealtime(holdTime);

        // Fade OUT
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Dọn dẹp
        if (shockwave != null)
            shockwave.StopWarning();
    }

    // ──────────────────────────────────────────────────────────────
    IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.blocksRaycasts = (to > 0.5f);
        float t = 0f;
        while (t < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = to;
        canvasGroup.blocksRaycasts = false; // luôn không block input
    }
}
