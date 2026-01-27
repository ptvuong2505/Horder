using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float timeBetweenSpawns = 0.5f;
    private float currentTimeBetweenSpawns;

    public static EnemyManager instance;

    private void Awake()
    {
         if (instance == null)
         {
             instance = this;
         }
         else
         {
             Destroy(gameObject);
        }
    }
}
