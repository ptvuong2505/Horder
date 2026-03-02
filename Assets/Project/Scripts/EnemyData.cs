using UnityEngine;

public enum EnemyType
{
    Normal,
    Ranged,
    Exploder,
    Tank,
    Swarm
}

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public GameObject prefab;
    public int hp;
    public float speed;
    public int damage = 10;
    public EnemyType enemyType = EnemyType.Normal;

    [Header("Ranged Settings — dùng cho EnemyType.Ranged")]
    public GameObject projectilePrefab;
    public float attackRange = 6f;
    public float fireRate = 1.5f;

    [Header("Exploder Settings — dùng cho EnemyType.Exploder")]
    public float explosionRange = 2f;
    public int explosionDamage = 30;
    public float explosionRadius = 3f;
}
