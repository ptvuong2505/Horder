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
/// <summary>
/// LevelConfig
/// Dữ liệu cấu hình cho 1 level:
/// - Danh sách waves (mỗi wave gồm nhiều EnemySpawnInfo)
/// - Quy tắc tính điểm/coins
/// - (Tuỳ chọn) boss spawn sau wave nhất định
/// 
/// bossSpawnAfterWave:
/// - -1 nghĩa là spawn boss sau wave cuối.
/// - 0 nghĩa là spawn boss sau wave đầu tiên (wave index 0).
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string levelName = "Level 1";
    public List<WaveConfig> waves = new List<WaveConfig>();

    // Coins = meta currency (mua súng ở menu/chọn level)
    public int coinsPerKill = 5;

    [Header("In-run Currency (Scrap)")]
    public int scrapPerKill = 1;
    public int scrapBonusPerWave = 3;

    [Header("Boss Settings")]
    public bool hasBoss = false;                    // Level này có boss không?
    public GameObject bossPrefab;                    // Prefab boss (gắn BossEnemy script)
    public int bossSpawnAfterWave = -1;             // Spawn boss SAU wave nào (-1 = sau wave cuối)
}
