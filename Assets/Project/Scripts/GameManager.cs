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

    const string SavedCoinsKey = "SavedCoins";

    [Header("References")]
    public EnemySpawner enemySpawner;
    public GunShopUI gunShopUI;          // Gán GunShopUI panel

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
        coins = PlayerPrefs.GetInt(SavedCoinsKey, 0);
    }

    void Start()
    {
        // Player xuat hien mac dinh khi sence chay
        Player player = FindObjectOfType<Player>();

        PlayerData data = PlayerSelectManager.Instance?.GetPreferredOrDefaultPlayer();

        if(data != null)
            player.Initialize(data);

        Debug.Log($"[GameManager] Spawned player: {data.playerName} with prefab {data.playerPrefab.name}");

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

        // Hiện upgrade selection (nếu có UpgradeManager).
        // Gun shop chỉ mở thủ công bằng phím G.
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ShowUpgradeSelection();
        }

        Debug.Log($"[GameManager] Wave {currentWave} clear!");
    }

    // Giữ lại để tương thích với EnemyManager event
    void HandleAllEnemiesDead() { }

    // ──────────────────────────────────────────────
    //  Khi hết tất cả wave → Level Clear
    // ──────────────────────────────────────────────
    public void HandleAllWavesCompleted()
    {
        state = GameState.LevelClear;
        OnStateChanged?.Invoke(state);
        Debug.Log("[GameManager] Level Clear! Chuyển sang màn kế tiếp...");

        // Lưu coins kiếm được vào SaveSystem
        if (PlayerSelectManager.Instance != null)
            PlayerSelectManager.Instance.AddGold(coins);

        ShowLevelClearMessage();

        StartCoroutine(LevelClearSequence());
    }

    IEnumerator LevelClearSequence()
    {
        float delay = 3f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelClear();
            float audioLen = AudioManager.Instance.GetLevelClearClipLength();
            if (audioLen > delay) delay = audioLen;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(3f, delay));
        LoadNextLevel();
    }

    void ShowLevelClearMessage()
    {
        GameObject canvasObj = new GameObject("LevelClearCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // Thêm nền đen mờ
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        bgImage.rectTransform.anchorMin = Vector2.zero;
        bgImage.rectTransform.anchorMax = Vector2.one;
        bgImage.rectTransform.offsetMin = Vector2.zero;
        bgImage.rectTransform.offsetMax = Vector2.zero;

        // Thêm Text chúc mừng
        GameObject textObj = new GameObject("CongratsText");
        textObj.transform.SetParent(canvasObj.transform, false);
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Chúc mừng thành công!";
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.yellow;
        text.fontSize = 80;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.localPosition = Vector3.zero;
        rect.sizeDelta = new Vector2(1000, 200);
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
        PlayerPrefs.SetInt(SavedCoinsKey, coins);
        PlayerPrefs.Save();
    }

    /// <summary>Trừ coin (dùng khi mua súng hoặc upgrade). Trả về true nếu đủ coin.</summary>
    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;
        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        PlayerPrefs.SetInt(SavedCoinsKey, coins);
        PlayerPrefs.Save();
        return true;
    }

    public static void ClearSavedCoins()
    {
        PlayerPrefs.DeleteKey(SavedCoinsKey);
        PlayerPrefs.Save();
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

        // Lưu coins kiếm được vào SaveSystem
        if (PlayerSelectManager.Instance != null)
            PlayerSelectManager.Instance.AddGold(coins);

        // Lưu coins để GameOverScene hiển thị
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
