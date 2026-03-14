using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager – Singleton quản lý toàn bộ vòng đời game:
/// wave, điểm số, thắng/thua.
/// Gắn vào GameObject "GameManager" trong scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public EnemySpawner enemySpawner;
    public GunShopUI gunShopUI;          // Gán GunShopUI panel

    [Header("Level Config")]
    public LevelConfig levelConfig;     // Gán cùng LevelConfig với EnemySpawner    // ──────────────────────────────────────────────
    //  Score
    // ──────────────────────────────────────────────
    private int score = 0;
    public int Score => score;
    public event Action<int> OnScoreChanged;   // (newScore)

    // ──────────────────────────────────────────────
    //  Coins (tiền rơi từ enemy)
    // ──────────────────────────────────────────────
    private int coins = 0;
    public int Coins => coins;
    public event Action<int> OnCoinsChanged;   // (newCoins)

    // ──────────────────────────────────────────────
    //  Wave
    // ──────────────────────────────────────────────
    private int currentWave = 0;
    public int CurrentWave => currentWave;
    public int TotalWaves => enemySpawner != null ? enemySpawner.TotalWaves : 0;
    public event Action<int, int> OnWaveChanged;   // (currentWave, totalWaves)

    // ──────────────────────────────────────────────
    //  Game State
    // ──────────────────────────────────────────────
    public enum GameState { Playing, WaveClear, LevelClear, GameOver }
    private GameState state = GameState.Playing;
    public GameState State => state;
    public event Action<GameState> OnStateChanged;

    // ──────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Đọc coin đã lưu từ lần chơi trước
        coins = PlayerPrefs.GetInt("SavedCoins", 0);
    }

    void Start()
    {
        // Đăng ký lắng nghe EnemyManager
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;        // Đăng ký lắng nghe EnemySpawner
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted += HandleWaveStarted;
            enemySpawner.OnAllWavesCompleted += HandleAllWavesCompleted;
        }
        // EnemySpawner tự Start() → không cần gọi StartNextWave() ở đây

        // Đồng bộ UI coin ngay khi vào scene (coin có thể đã được load từ PlayerPrefs)
        OnCoinsChanged?.Invoke(coins);
    }

    void OnDestroy()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAllEnemiesDead -= HandleAllEnemiesDead;

        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted -= HandleWaveStarted;
            enemySpawner.OnAllWavesCompleted -= HandleAllWavesCompleted;
        }
    }

    // ──────────────────────────────────────────────
    //  Điều khiển Wave
    // ──────────────────────────────────────────────
    void StartNextWave()
    {
        if (enemySpawner != null)
            enemySpawner.StartNextWave();
    }

    void HandleWaveStarted(int waveIndex)
    {
        currentWave = waveIndex + 1;  // Hiển thị từ 1
        state = GameState.Playing;
        OnWaveChanged?.Invoke(currentWave, TotalWaves);
        OnStateChanged?.Invoke(state);
        Debug.Log($"[GameManager] Wave {currentWave}/{TotalWaves} bắt đầu!");
    }    // ──────────────────────────────────────────────
    //  Khi hết enemy trong wave (gọi từ EnemySpawner)
    // ──────────────────────────────────────────────
    public void HandleWaveClear()
    {
        if (state != GameState.Playing) return;

        if (levelConfig != null)
            AddScore(levelConfig.bonusScorePerWave);

        state = GameState.WaveClear;
        OnStateChanged?.Invoke(state);

        // SFX wave clear
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWaveClear();

        // Hiện upgrade selection (nếu có UpgradeManager)
        // Sau khi chọn upgrade xong → mở GunShop
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeSelected += OpenGunShopAfterUpgrade;
            UpgradeManager.Instance.ShowUpgradeSelection();
        }
        else
        {
            // Không có upgrade → mở thẳng shop
            OpenGunShop();
        }

        Debug.Log($"[GameManager] Wave {currentWave} clear! +{levelConfig?.bonusScorePerWave} điểm thưởng");
    }

    // ──────────────────────────────────────────────
    //  Gun Shop
    // ──────────────────────────────────────────────
    void OpenGunShopAfterUpgrade()
    {
        // Hủy đăng ký để không bị gọi nhiều lần
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeSelected -= OpenGunShopAfterUpgrade;

        OpenGunShop();
    }

    void OpenGunShop()
    {
        if (gunShopUI != null)
            gunShopUI.OpenShop();
        else
            Debug.Log("[GameManager] Không có GunShopUI – bỏ qua shop.");
    }

    // Giữ lại để tương thích với EnemyManager event
    void HandleAllEnemiesDead() { }

    // ──────────────────────────────────────────────
    //  Khi hết tất cả wave → Level Clear
    // ──────────────────────────────────────────────
    void HandleAllWavesCompleted()
    {
        state = GameState.LevelClear;
        OnStateChanged?.Invoke(state);
        Debug.Log("[GameManager] Level Clear! Chuyển sang màn kế tiếp...");

        Invoke(nameof(LoadNextLevel), 3f);
    }

    void LoadNextLevel()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            SceneManager.LoadScene("Menu"); // Hết game → về Menu
    }    // ──────────────────────────────────────────────
    //  Điểm số
    // ──────────────────────────────────────────────
    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    // ──────────────────────────────────────────────
    //  Coins
    // ──────────────────────────────────────────────
    public void AddCoins(int amount)
    {
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
        PlayerPrefs.SetInt("SavedCoins", coins);
        PlayerPrefs.Save();
    }

    /// <summary>Trừ coin (dùng khi mua súng hoặc upgrade). Trả về true nếu đủ coin.</summary>
    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;
        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        PlayerPrefs.SetInt("SavedCoins", coins);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// Gọi từ Enemy khi bị tiêu diệt: cộng điểm + coins.
    /// </summary>
    public void RegisterKill()
    {
        if (levelConfig != null)
        {
            AddScore(levelConfig.scorePerKill);
            AddCoins(levelConfig.coinsPerKill);
        }
    }

    // ──────────────────────────────────────────────
    //  Game Over (Player chết)
    // ──────────────────────────────────────────────
    public void TriggerGameOver()
    {
        if (state == GameState.GameOver) return;
        state = GameState.GameOver;
        OnStateChanged?.Invoke(state);

        // SFX game over
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameOver();

        Debug.Log($"[GameManager] Game Over! Score: {score}");        // Lưu điểm & coins để GameOverScene hiển thị
        PlayerPrefs.SetInt("FinalScore", score);
        PlayerPrefs.SetInt("FinalCoins", coins);
        PlayerPrefs.Save();

        Invoke(nameof(LoadGameOverScene), 2f);
    }

    void LoadGameOverScene()
    {
        SceneManager.LoadScene("bg_game_over");
    }
}
