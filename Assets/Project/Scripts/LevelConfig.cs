using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public EnemyData enemyData;
    public int count;
}

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    public List<EnemySpawnInfo> enemies;
}
