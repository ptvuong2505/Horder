using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Phát introduction audio/video lúc game start, rồi load Menu scene.
/// Gắn lên một GameObject trong Scene 0 (khởi động).
/// </summary>
public class IntroductionManager : MonoBehaviour
{
    public static bool HasPlayedIntroThisSession { get; private set; }

    [Tooltip("Scene menu cần load sau khi introduction xong")]
    public string menuSceneName = "Menu";

    [Header("Introduction Source")]
    [Tooltip("VideoPlayer phát intro video. Nếu để trống sẽ fallback sang audio nếu có.")]
    public VideoPlayer introVideoPlayer;

    [Tooltip("Nếu true, dùng AudioManager phát audio thay cho video")]
    public bool useAudio = false;

    [Tooltip("Delay thêm trước khi load menu (giây)")]
    public float delayBeforeLoadMenu = 0.5f;

    private float introLength = 0f;
    private bool _videoFinished;
    private bool _started;

    void Awake()
    {
        if (!useAudio && introVideoPlayer == null)
            introVideoPlayer = FindFirstObjectByType<VideoPlayer>(FindObjectsInactive.Include);
    }

    void Start()
    {
        if (_started) return;
        _started = true;
        StartCoroutine(PlayIntroThenLoadMenu());
    }

    IEnumerator PlayIntroThenLoadMenu()
    {
        // Ưu tiên video nếu có VideoPlayer và không ép dùng audio.
        if (!useAudio && introVideoPlayer != null)
        {
            yield return StartCoroutine(PlayVideoIntro());
        }
        // Fallback: phát audio intro từ AudioManager.
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayIntroduction();
            introLength = AudioManager.Instance.GetIntroductionClipLength();
            Debug.Log($"[IntroductionManager] Phát introduction audio ({introLength}s)");
            yield return new WaitForSecondsRealtime(introLength);
        }
        else
        {
            Debug.LogWarning("[IntroductionManager] Không có VideoPlayer và cũng không có AudioManager/introductionAudio. Dùng fallback 1.5s rồi vào Menu.");
            yield return new WaitForSecondsRealtime(1.5f);
        }

        // Delay thêm nếu cần
        if (delayBeforeLoadMenu > 0)
            yield return new WaitForSecondsRealtime(delayBeforeLoadMenu);

        // Load menu scene
        HasPlayedIntroThisSession = true;
        Debug.Log($"[IntroductionManager] Load scene: {menuSceneName}");
        SceneManager.LoadScene(menuSceneName);
    }

    IEnumerator PlayVideoIntro()
    {
        _videoFinished = false;
        if (introVideoPlayer.clip == null && string.IsNullOrEmpty(introVideoPlayer.url))
        {
            Debug.LogWarning("[IntroductionManager] VideoPlayer chưa có clip/url. Bỏ qua intro video.");
            yield break;
        }

        introVideoPlayer.isLooping = false;
        introVideoPlayer.loopPointReached += OnVideoLoopPointReached;

        // Chuẩn bị video trước khi play để tránh giật frame đầu.
        if (!introVideoPlayer.isPrepared)
        {
            introVideoPlayer.Prepare();
            while (!introVideoPlayer.isPrepared)
                yield return null;
        }

        introVideoPlayer.Play();
        Debug.Log("[IntroductionManager] Phát introduction video...");

        // Đợi tới khi video kết thúc.
        if (introVideoPlayer.clip != null)
        {
            float waitLimit = (float)introVideoPlayer.clip.length + 0.5f;
            float elapsed = 0f;
            while (!_videoFinished && elapsed < waitLimit)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            while (!_videoFinished && introVideoPlayer.isPlaying)
                yield return null;
        }

        introVideoPlayer.loopPointReached -= OnVideoLoopPointReached;
    }

    void OnDisable()
    {
        if (introVideoPlayer != null)
            introVideoPlayer.loopPointReached -= OnVideoLoopPointReached;
    }

    void OnVideoLoopPointReached(VideoPlayer vp)
    {
        _videoFinished = true;
    }
}
