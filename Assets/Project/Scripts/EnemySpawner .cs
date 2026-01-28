using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public LevelConfig levelConfig;
    public Transform player;

    public float spawnInterval = 1f;
    public float spawnDistance = 10f;

    private int enemyIndex = 0;   // đang spawn loại enemy nào
    private int spawnedCount = 0; // đã spawn bao nhiêu con loại đó
    private float timer;

    void Update()
    {
        if (levelConfig == null || player == null) return;
        if (enemyIndex >= levelConfig.enemies.Count) return; // spawn xong toàn bộ level

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        EnemySpawnInfo info = levelConfig.enemies[enemyIndex];

        // Nếu đã spawn đủ số lượng của enemy hiện tại
        if (spawnedCount >= info.count)
        {
            enemyIndex++;      // chuyển sang enemy tiếp theo
            spawnedCount = 0;
            return;
        }

        Vector3 spawnPos = GetRandomPositionAroundPlayer();

        Instantiate(
            info.enemyData.prefab,
            spawnPos,
            Quaternion.identity
        );

        spawnedCount++;
    }

    Vector3 GetRandomPositionAroundPlayer()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        return player.position + new Vector3(dir.x, dir.y, 0) * spawnDistance;
    }
}
