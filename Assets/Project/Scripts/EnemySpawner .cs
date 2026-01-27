using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public LevelConfig levelConfig;
    public Transform[] spawnPoints;

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        foreach (var info in levelConfig.enemies)
        {
            for (int i = 0; i < info.count; i++)
            {
                Transform p = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject obj = Instantiate(info.enemyData.prefab, p.position, Quaternion.identity);

                Enemy e = obj.GetComponent<Enemy>();
                e.maxHealth = info.enemyData.maxHealth;
                e.speed = info.enemyData.speed;
            }
        }
    }
}
