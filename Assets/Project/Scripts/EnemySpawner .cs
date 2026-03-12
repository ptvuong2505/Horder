using System;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Config")]
    public LevelConfig levelConfig;
    public Transform player;
    public float spawnDistance = 10f;

    public event Action<int> OnWaveStarted;
    public event Action OnAllWavesCompleted;

    private int currentWaveIndex = -1;
    private bool isSpawning = false;
    private int totalSpawnedThisWave = 0;   // đã spawn bao nhiêu con trong wave
    private int totalInThisWave = 0;        // tổng số con cần spawn trong wave

    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => levelConfig != null ? levelConfig.waves.Count : 0;

    void Start()
    {
        StartNextWave();
    }

    // ──────────────────────────────────────────────
    public void StartNextWave()
    {
        if (levelConfig == null)
        {
            Debug.LogError("[EnemySpawner] Chưa gán LevelConfig!");
            return;
        }

        currentWaveIndex++;

        if (currentWaveIndex >= levelConfig.waves.Count)
        {
            OnAllWavesCompleted?.Invoke();
            Debug.Log("[EnemySpawner] Đã hết tất cả wave!");
            return;
        }

        if (!isSpawning)
            StartCoroutine(SpawnWave(levelConfig.waves[currentWaveIndex]));
    }

    // ──────────────────────────────────────────────
    IEnumerator SpawnWave(WaveConfig wave)
    {
        isSpawning = true;

        // Tính tổng enemy của wave để EnemyManager theo dõi
        totalSpawnedThisWave = 0;
        totalInThisWave = 0;
        foreach (var info in wave.enemies)
            totalInThisWave += info.count;

        Debug.Log($"[EnemySpawner] Wave {currentWaveIndex + 1}: chờ {wave.delayBeforeWave}s rồi spawn {totalInThisWave} enemy...");

        yield return new WaitForSeconds(wave.delayBeforeWave);

        OnWaveStarted?.Invoke(currentWaveIndex);

        // Spawn từng con
        foreach (EnemySpawnInfo info in wave.enemies)
        {
            if (info.enemyData == null || info.enemyData.prefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Wave {currentWaveIndex + 1}: enemyData hoặc prefab bị NULL!");
                continue;
            }

            for (int i = 0; i < info.count; i++)
            {
                Vector3 pos = GetSpawnPosition();
                Instantiate(info.enemyData.prefab, pos, Quaternion.identity);
                totalSpawnedThisWave++;
                Debug.Log($"[EnemySpawner] Spawned enemy {totalSpawnedThisWave}/{totalInThisWave} tại {pos}");
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        isSpawning = false;
        Debug.Log($"[EnemySpawner] Wave {currentWaveIndex + 1} đã spawn xong. Chờ hết enemy...");

        // Chờ đến khi không còn enemy nào trên scene
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

        Debug.Log($"[EnemySpawner] Wave {currentWaveIndex + 1} CLEAR! Sang wave tiếp...");

        // Báo GameManager (nếu có) → sẽ trigger upgrade UI
        if (GameManager.Instance != null)
            GameManager.Instance.HandleWaveClear();

        // Chờ upgrade selection hoàn tất (nếu UpgradeManager đang show)
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsSelecting)
        {
            yield return new WaitUntil(() => !UpgradeManager.Instance.IsSelecting);
        }

        // ═══ BOSS SPAWN ═══
        // Kiểm tra xem có cần spawn boss sau wave này không
        int bossAfterWave = levelConfig.bossSpawnAfterWave;
        if (bossAfterWave < 0) bossAfterWave = levelConfig.waves.Count - 1; // -1 = sau wave cuối

        if (levelConfig.hasBoss && levelConfig.bossPrefab != null && currentWaveIndex == bossAfterWave)
        {
            Debug.Log("[EnemySpawner] ⚠️ BOSS INCOMING!");

            // Hiển thị cảnh báo boss 2 giây
            BossWarningUI bossWarning = FindFirstObjectByType<BossWarningUI>();
            if (bossWarning != null)
                yield return StartCoroutine(bossWarning.ShowAndHide(2f));
            else
                yield return new WaitForSecondsRealtime(2f);

            // Spawn boss ở vị trí xa player
            Vector3 bossPos = GetSpawnPosition();
            GameObject boss = Instantiate(levelConfig.bossPrefab, bossPos, Quaternion.identity);
            Debug.Log($"[EnemySpawner] 👑 BOSS SPAWNED tại {bossPos}");

            // Chờ boss bị tiêu diệt
            yield return new WaitUntil(() => boss == null);
            Debug.Log("[EnemySpawner] 👑 BOSS DEFEATED!");

            // Chờ thêm minion chết hết
            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

            // Báo wave clear cho boss wave
            if (GameManager.Instance != null)
                GameManager.Instance.HandleWaveClear();
        }

        // Chờ 3s trước wave tiếp (dùng Realtime để không bị ảnh hưởng bởi TimeScale)
        yield return new WaitForSecondsRealtime(3f);

        StartNextWave();
    }

    // ──────────────────────────────────────────────
    Vector3 GetSpawnPosition()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        Vector3 center = player != null ? player.position : Vector3.zero;
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        return center + new Vector3(dir.x, dir.y, 0) * spawnDistance;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnDistance);
    }
}

