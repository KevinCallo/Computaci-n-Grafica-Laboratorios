using UnityEngine;
using System.Collections;

public class GrenadeProjectile : MonoBehaviour
{
    [Header("Physics")]
    public Vector3 throwDirection;
    public float speed = 20f;
    public float fuseTime = 2f;

    [Header("Damage")]
    public float damage = 60f;
    public float blastRadius = 6f;

    [Header("Effects")]
    public ParticleSystem explosionEffect;
    public AudioSource audioSource;
    public AudioClip explodeClip;
    public GameObject blastRadiusIndicator;

    private Rigidbody rb;
    private bool exploded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = throwDirection * speed;
            rb.useGravity = true;
        }
        if (blastRadiusIndicator) blastRadiusIndicator.transform.localScale = Vector3.one * blastRadius * 2f;
        StartCoroutine(FuseCoroutine());
    }

    IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    void OnCollisionEnter(Collision col)
    {
        // Bounces off ground naturally via rigidbody, explodes on timer
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        // Damage all enemies in radius
        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / blastRadius);
                enemy.TakeDamage(damage * falloff);
            }
            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / blastRadius);
                player.TakeDamage(damage * falloff * 0.3f); // self-damage reduced
            }
        }

        if (explosionEffect) { explosionEffect.transform.parent = null; explosionEffect.Play(); Destroy(explosionEffect.gameObject, 3f); }
        if (audioSource && explodeClip) audioSource.PlayOneShot(explodeClip);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, blastRadius);
    }
}
