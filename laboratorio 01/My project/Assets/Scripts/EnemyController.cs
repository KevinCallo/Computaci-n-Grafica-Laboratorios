using UnityEngine;
using System.Collections;

public enum EnemyType { Normal, Fast, Tank }

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Config")]
    public EnemyType enemyType = EnemyType.Normal;
    public float maxHealth = 40f;
    public float currentHealth;
    public int scoreValue = 100;
    public float moveSpeed = 4f;
    public float fireRate = 2.5f;
    public float detectionRange = 25f;
    public float fireRange = 18f;

    [Header("Headshot Zone")]
    public Transform headshotPoint; // assigned in prefab - top of enemy
    public float headshotRadius = 0.4f;

    [Header("Projectile")]
    public GameObject enemyBulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 14f;
    public float bulletDamage = 18f;

    [Header("AI")]
    public float dodgeInterval = 1.5f;
    public float dodgeForce = 6f;

    [Header("Effects")]
    public ParticleSystem hitEffect;
    public ParticleSystem deathEffect;
    public AudioSource audioSource;
    public AudioClip hurtClip;
    public AudioClip deathClip;
    public AudioClip fireClip;

    [Header("UI")]
    public GameObject healthBarPrefab;
    private EnemyHealthBar healthBar;

    private Transform player;
    private float fireCooldown;
    private float dodgeTimer;
    private Vector3 dodgeDirection;
    private bool isDead = false;
    private bool isFlashing = false;
    private Renderer[] renderers;
    private Color[] originalColors;
    private CharacterController cc;
    private float verticalVelocity = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    public void Initialize(int wave)
    {
        float waveScale = 1f + (wave - 1) * 0.15f;
        maxHealth *= waveScale;
        currentHealth = maxHealth;
        moveSpeed *= 1f + (wave - 1) * 0.08f;
        bulletDamage *= 1f + (wave - 1) * 0.1f;
        fireRate = Mathf.Max(0.8f, fireRate - (wave - 1) * 0.1f);
        fireCooldown = Random.Range(0f, fireRate);
        dodgeTimer = Random.Range(0f, dodgeInterval);
        player = GameObject.FindWithTag("Player")?.transform;

        // Spawn health bar
        if (healthBarPrefab)
        {
            GameObject hb = Instantiate(healthBarPrefab);
            healthBar = hb.GetComponent<EnemyHealthBar>();
            if (healthBar) healthBar.Setup(transform, maxHealth);
        }
    }

    void Update()
    {
        if (isDead || player == null || !GameManager.Instance.gameActive) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return;

        HandleMovement(dist);
        HandleFiring(dist);
        HandleDodge();
        ApplyGravity();
    }

    void HandleMovement(float dist)
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0f;

        // Rotate toward player
        if (dirToPlayer != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 5f);

        // Move toward player if not in fire range
        Vector3 moveDir = Vector3.zero;
        if (dist > fireRange * 0.7f)
            moveDir = dirToPlayer;
        else if (dist < 3f)
            moveDir = -dirToPlayer; // back off if too close

        // Add dodge
        moveDir += dodgeDirection * 0.5f;
        moveDir.y = 0f;

        if (cc != null)
            cc.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
    }

    void HandleFiring(float dist)
    {
        if (dist > fireRange) return;
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            Fire();
            fireCooldown = fireRate;
            if (enemyType == EnemyType.Tank) { Fire(0.25f); Fire(-0.25f); } // spread shot
        }
    }

    void Fire(float spreadAngle = 0f)
    {
        if (enemyBulletPrefab == null || firePoint == null || player == null) return;

        Vector3 dir = (player.position + Vector3.up * 1f - firePoint.position).normalized;
        if (spreadAngle != 0f) dir = Quaternion.Euler(0f, spreadAngle * Mathf.Rad2Deg, 0f) * dir;

        GameObject b = Instantiate(enemyBulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        Projectile proj = b.GetComponent<Projectile>();
        if (proj) { proj.speed = bulletSpeed; proj.damage = bulletDamage; proj.isPlayerProjectile = false; }
        PlaySound(fireClip);
    }

    void HandleDodge()
    {
        dodgeTimer -= Time.deltaTime;
        if (dodgeTimer <= 0f && enemyType != EnemyType.Tank)
        {
            dodgeTimer = dodgeInterval + Random.Range(-0.3f, 0.3f);
            float angle = Random.Range(60f, 120f) * (Random.value < 0.5f ? 1f : -1f);
            Vector3 fwd = (player.position - transform.position).normalized;
            dodgeDirection = Quaternion.Euler(0f, angle, 0f) * fwd;
            dodgeDirection.y = 0f;
        }
    }

    void ApplyGravity()
    {
        if (cc == null) return;
        if (cc.isGrounded) verticalVelocity = -2f;
        else verticalVelocity += Physics.gravity.y * Time.deltaTime;
        cc.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    public void TakeDamage(float dmg, bool headshot = false)
    {
        if (isDead) return;
        float actualDmg = headshot ? dmg * 2f : dmg;
        currentHealth -= actualDmg;
        if (healthBar) healthBar.UpdateHealth(currentHealth);
        if (!isFlashing) StartCoroutine(FlashRed());
        PlaySound(hurtClip);
        if (hitEffect) hitEffect.Play();

        if (currentHealth <= 0f) Die(headshot);
    }

    void Die(bool headshot)
    {
        isDead = true;
        if (deathEffect) { deathEffect.transform.parent = null; deathEffect.Play(); Destroy(deathEffect.gameObject, 2f); }
        PlaySound(deathClip);
        if (healthBar) Destroy(healthBar.gameObject);
        GameManager.Instance.EnemyKilled(scoreValue, enemyType.ToString(), headshot);
        Destroy(gameObject);
    }

    IEnumerator FlashRed()
    {
        isFlashing = true;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = originalColors[i];
        isFlashing = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fireRange);
        if (headshotPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(headshotPoint.position, headshotRadius);
        }
    }
}
