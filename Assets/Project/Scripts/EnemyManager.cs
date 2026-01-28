using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private List<GameObject> aliveEnemies = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterEnemy(GameObject enemy)
    {
        aliveEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    public int GetAliveCount()
    {
        return aliveEnemies.Count;
    }
}
