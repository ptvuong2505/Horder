using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public LevelConfig levelConfig;
    public Transform player;

    public float spawnInterval = 2f;
    public float spawnDistance = 10f;
    public int maxEnemyAlive = 10; // 🔥 CHỐT CHẶN

    private int enemyIndex = 0;
    private int spawnedCount = 0;
    private float timer;

    void Update()
    {
        if (levelConfig == null || player == null) return;
        if (enemyIndex >= levelConfig.enemies.Count) return;

        if (EnemyManager.Instance.GetAliveCount() >= maxEnemyAlive)
            return;

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

        if (spawnedCount >= info.count)
        {
            enemyIndex++;
            spawnedCount = 0;
            return;
        }

        Vector3 spawnPos = GetRandomPositionAroundPlayer();

        GameObject enemy = Instantiate(
            info.enemyData.prefab,
            spawnPos,
            Quaternion.identity
        );

        EnemyManager.Instance.RegisterEnemy(enemy);
        spawnedCount++;
    }

    Vector3 GetRandomPositionAroundPlayer()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        return player.position + new Vector3(dir.x, dir.y, 0) * spawnDistance;
    }
}
