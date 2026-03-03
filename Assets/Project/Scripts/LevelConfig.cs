using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────
//  Dữ liệu 1 loại enemy trong wave
// ─────────────────────────────────────────────────────────
[System.Serializable]
public class EnemySpawnInfo
{
    public EnemyData enemyData;
    public int count;
}

// ─────────────────────────────────────────────────────────
//  Dữ liệu 1 wave: gồm nhiều loại enemy
// ─────────────────────────────────────────────────────────
[System.Serializable]
public class WaveConfig
{
    public string waveName = "Wave 1";
    public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
    public float spawnInterval = 1f;    // Giây giữa 2 lần spawn trong wave này
    public float delayBeforeWave = 2f;  // Giây chờ trước khi wave bắt đầu
}

// ─────────────────────────────────────────────────────────
//  LevelConfig: danh sách các wave + boss config
//  Tạo asset: chuột phải > Create > Game/Level Config
// ─────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string levelName = "Level 1";
    public List<WaveConfig> waves = new List<WaveConfig>();
    public int scorePerKill = 10;       // Điểm mỗi khi giết 1 enemy
    public int coinsPerKill = 5;        // Coins rơi mỗi khi giết 1 enemy
    public int bonusScorePerWave = 50;  // Điểm thưởng hoàn thành wave

    [Header("Boss Settings")]
    public bool hasBoss = false;                    // Level này có boss không?
    public GameObject bossPrefab;                    // Prefab boss (gắn BossEnemy script)
    public int bossSpawnAfterWave = -1;             // Spawn boss SAU wave nào (-1 = sau wave cuối)
}
