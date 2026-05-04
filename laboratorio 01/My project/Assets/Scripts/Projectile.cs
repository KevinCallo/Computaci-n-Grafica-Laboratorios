using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Physics Values")]
    public float speed = 30f;
    public float lifetime = 4f;
    public float damage = 25f;
    public float gravity = 0f; // 0 = hitscan-like, positive = arc

    [Header("Behavior")]
    public bool isPlayerProjectile = true;
    public bool isPiercing = false;
    public bool isHeadshotCapable = false;

    [Header("Effects")]
    public ParticleSystem hitEffect;
    public TrailRenderer trail;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = transform.forward * speed;
            rb.useGravity = gravity > 0f;
            if (gravity > 0f) rb.linearVelocity += Vector3.down * gravity;
        }
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollisionObject(collision.collider);
    }

    void HandleCollision(Collider other)
    {
        HandleCollisionObject(other);
    }

    void HandleCollisionObject(Collider other)
    {
        if (other.isTrigger) return;

        if (isPlayerProjectile)
        {
            // Check for headshot first
            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                bool headshot = false;
                if (isHeadshotCapable && enemy.headshotPoint != null)
                {
                    float distToHead = Vector3.Distance(transform.position, enemy.headshotPoint.position);
                    headshot = distToHead < enemy.headshotRadius;
                }
                enemy.TakeDamage(damage, headshot);
                SpawnHitEffect();
                if (!isPiercing) Destroy(gameObject);
                return;
            }
        }
        else
        {
            // Enemy bullet hitting player
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                SpawnHitEffect();
                Destroy(gameObject);
                return;
            }
        }

        // Hit environment
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy") && !other.CompareTag("Projectile"))
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    void SpawnHitEffect()
    {
        if (hitEffect)
        {
            hitEffect.transform.parent = null;
            hitEffect.Play();
            Destroy(hitEffect.gameObject, 2f);
        }
    }
}
