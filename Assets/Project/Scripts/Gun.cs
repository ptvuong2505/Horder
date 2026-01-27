using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject muzzle;
    [SerializeField] Transform muzzlePosition;
    [SerializeField] GameObject projectile;

    [Header("Config")]
    [SerializeField] float fireDistance = 10;
    [SerializeField] float fireRate = 0.5f;

    Transform player;
    Vector2 offset;

    private float timeSinceLastShot = 0f;
    Transform closestEnemy;
    Animation anim;

    private void Start()
    {
        anim = GetComponent<Animation>();
        timeSinceLastShot = fireRate;
        player = GameObject.Find("Player").transform;
        offset = new Vector2(1f,0);
    }

    private void Update()
    {
        transform.position = (Vector2)player.position + offset;
    }

    void FindClosestEnemy()
    {
        closestEnemy = null;
        
    }
}
