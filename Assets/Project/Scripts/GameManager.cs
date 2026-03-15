using Assets.Project.Scripts;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager (Singleton)
/// Quản lý vòng đời 1 màn chơi:
/// - Theo dõi Coins/Scrap/Wave và phát event cho UI.
/// - Nhận tín hiệu từ EnemySpawner + EnemyManager để biết wave bắt đầu/kết thúc.
/// - Khi WaveClear: play SFX, mở UI upgrade (pause game) và thưởng Scrap.
/// - Khi LevelClear/GameOver: chuyển scene.
/// 
/// Dữ liệu được lưu qua PlayerPrefs khi GameOver:
/// - FinalCoins
/// - FinalScrap
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public EnemySpawner enemySpawner;

    [Header("Level Config")]
    public LevelConfig levelConfig;     // Gán cùng LevelConfig với EnemySpawner

    // ──────────────────────────────────────────────
    //  NOTE: Score system removed per request.
    //  Nếu cần hiển thị tiến trình trong run, ưu tiên dùng Wave/KillCount/TimeSurvived thay vì điểm.
    // ──────────────────────────────────────────────

    // ──────────────────────────────────────────────
    //  Coins (tiền rơi từ enemy)
    // ──────────────────────────────────────────────
    // ──────────────────────────────────────────────
    private int coins = 0;
    public int Coins => coins;
    public event Action<int> OnCoinsChanged;   // (newCoins)

    // ──────────────────────────────────────────────
    //  Scrap (tiền trong trận / in-run currency)
    //  Dùng cho các quyết định trong run: reroll upgrade, mua item trong shop giữa trận...
    //  Coins vẫn giữ vai trò meta-currency (mua súng ở menu/chọn level).
    // ──────────────────────────────────────────────
    private int scrap = 0;
    public int Scrap => scrap;
    public event Action<int> OnScrapChanged;   // (newScrap)

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
    // Selected Player
    // ──────────────────────────────────────────────
    public PlayerData selectedPlayer;  // Fallback khi không có PlayerSelectManager


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Ưu tiên lấy player từ PlayerSelectManager; fallback về field selectedPlayer
        PlayerData data = (PlayerSelectManager.Instance != null && PlayerSelectManager.Instance.SelectedPlayer != null)
            ? PlayerSelectManager.Instance.SelectedPlayer
            : selectedPlayer;

        GameObject obj = Instantiate(data.playerPrefab);
        Player player = obj.GetComponent<Player>();

        player.Initialize(data);

        // Đăng ký lắng nghe EnemyManager
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAllEnemiesDead += HandleAllEnemiesDead;

        // Đăng ký lắng nghe EnemySpawner
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted += HandleWaveStarted;
            enemySpawner.OnAllWavesCompleted += HandleAllWavesCompleted;
        }
        // EnemySpawner tự Start() → không cần gọi StartNextWave() ở đây
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
    }

    // ──────────────────────────────────────────────
    //  Khi hết enemy trong wave (gọi từ EnemySpawner)
    // ──────────────────────────────────────────────
    public void HandleWaveClear()
    {
        if (state != GameState.Playing) return;

        if (levelConfig != null)
        {
            // Score removed
            AddScrap(levelConfig.scrapBonusPerWave);
        }

        state = GameState.WaveClear;
        OnStateChanged?.Invoke(state);

        // SFX wave clear
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWaveClear();

        // Hiện upgrade selection (nếu có UpgradeManager)
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ShowUpgradeSelection();

        Debug.Log($"[GameManager] Wave {currentWave} clear!");
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

        // Lưu coins kiếm được vào SaveSystem
        if (PlayerSelectManager.Instance != null)
            PlayerSelectManager.Instance.AddGold(coins);

        Invoke(nameof(LoadNextLevel), 3f);
        StartCoroutine(LevelClearSequence());
    }

    IEnumerator LevelClearSequence()
    {
        float delay = 3f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelClear();
            delay = AudioManager.Instance.GetLevelClearClipLength();
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, delay));
        LoadNextLevel();
    }

    void LoadNextLevel()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            SceneManager.LoadScene("Menu"); // Hết game → về Menu
    }

    // ──────────────────────────────────────────────
    //  Coins
    // ──────────────────────────────────────────────
    public void AddCoins(int amount)
    {
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
    }

    // ──────────────────────────────────────────────
    //  Scrap
    // ──────────────────────────────────────────────
    /// <summary>
    /// Add/Spend scrap trong run.
    /// amount có thể âm (chi tiêu).
    /// </summary>
    public void AddScrap(int amount)
    {
        scrap = Mathf.Max(0, scrap + amount);
        OnScrapChanged?.Invoke(scrap);
    }

    /// <summary>
    /// Thử chi scrap. Trả về true nếu đủ tiền và đã trừ.
    /// </summary>
    public bool TrySpendScrap(int amount)
    {
        if (amount <= 0) return true;
        if (scrap < amount) return false;
        AddScrap(-amount);
        return true;
    }

    /// <summary>
    /// Gọi từ Enemy khi bị tiêu diệt: cộng coins + scrap.
    /// </summary>
    public void RegisterKill()
    {
        if (levelConfig != null)
        {
            AddCoins(levelConfig.coinsPerKill);
            AddScrap(levelConfig.scrapPerKill);
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

        Debug.Log($"[GameManager] Game Over! Score: {score}");

        // Lưu coins kiếm được vào SaveSystem
        if (PlayerSelectManager.Instance != null)
            PlayerSelectManager.Instance.AddGold(coins);

        // Lưu điểm & coins để GameOverScene hiển thị
        PlayerPrefs.SetInt("FinalScore", score);
        Debug.Log($"[GameManager] Game Over! Coins: {coins}, Scrap: {scrap}");
        PlayerPrefs.SetInt("FinalCoins", coins);
        PlayerPrefs.SetInt("FinalScrap", scrap);
        PlayerPrefs.Save();

        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        float delay = 2f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
            delay = AudioManager.Instance.GetGameOverClipLength();
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, delay));
        LoadGameOverScene();
    }

    void LoadGameOverScene()
    {
        SceneManager.LoadScene("bg_game_over");
    }
}
