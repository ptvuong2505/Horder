using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public GameObject prefab;
    public int hp;
    public float speed;
}
