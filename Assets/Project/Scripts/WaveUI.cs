using TMPro;
using UnityEngine;

/// <summary>
/// WaveUI – Hiển thị thông báo wave trên màn hình.
/// Gắn vào Canvas > WaveUI object.
/// </summary>
public class WaveUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI waveText;        // "Wave 1 / 3"
    public TextMeshProUGUI waveAnnounce;    // "WAVE 1 START!" / "WAVE CLEAR!"
    public CanvasGroup announceGroup;       // CanvasGroup để fade in/out

    private float fadeDuration = 1f;
    private float displayDuration = 1.5f;
    private float fadeTimer = 0f;
    private bool isFading = false;
    private bool isFadeIn = false;

    void Start()
    {
        if (announceGroup != null)
            announceGroup.alpha = 0f;

        // Đăng ký lắng nghe GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged += UpdateWaveText;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged -= UpdateWaveText;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    void Update()
    {
        if (!isFading) return;

        fadeTimer += Time.deltaTime;

        if (isFadeIn)
        {
            // Fade in
            announceGroup.alpha = Mathf.Clamp01(fadeTimer / fadeDuration);
            if (fadeTimer >= fadeDuration + displayDuration)
            {
                isFadeIn = false;
                fadeTimer = 0f;
            }
        }
        else
        {
            // Fade out
            announceGroup.alpha = 1f - Mathf.Clamp01(fadeTimer / fadeDuration);
            if (fadeTimer >= fadeDuration)
            {
                isFading = false;
                announceGroup.alpha = 0f;
            }
        }
    }

    // ──────────────────────────────────────────────
    void UpdateWaveText(int current, int total)
    {
        if (waveText != null)
            waveText.text = $"Wave {current} / {total}";
    }

    void HandleStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Playing:
                int wave = GameManager.Instance.CurrentWave;
                ShowAnnounce($"WAVE {wave}");
                break;

            case GameManager.GameState.WaveClear:
                ShowAnnounce("WAVE CLEAR!");
                break;

            case GameManager.GameState.LevelClear:
                ShowAnnounce("LEVEL CLEAR!");
                break;
        }
    }

    void ShowAnnounce(string message)
    {
        if (waveAnnounce != null)
            waveAnnounce.text = message;

        if (announceGroup != null)
        {
            announceGroup.alpha = 0f;
            fadeTimer = 0f;
            isFadeIn = true;
            isFading = true;
        }
    }
}
