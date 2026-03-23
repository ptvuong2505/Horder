using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// PauseManager – Singleton quản lý trạng thái pause.
/// Gắn vào HUD prefab. Dùng Additive scene loading để giữ game state.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private string gameSceneName;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        gameSceneName = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f; // Đảm bảo timeScale bình thường khi bắt đầu
    }

    void Update()
    {
        // Nhấn ESC để toggle pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    /// <summary>
    /// Freeze game, load bg_pause scene phủ lên trên.
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        SceneManager.LoadScene("bg_pause", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Gọi từ BgPauseManager khi nhấn Resume.
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.UnloadSceneAsync("bg_pause");
    }

    /// <summary>
    /// Gọi từ BgPauseManager khi nhấn Restart.
    /// </summary>
    public void RestartGame()
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        PlayerPrefs.DeleteKey("FinalScrap");
        GameManager.ClearSavedCoins();
        GunShop.ClearSavedUnlocks();
        WeaponManager.ClearSavedEquippedGun();

        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
}
