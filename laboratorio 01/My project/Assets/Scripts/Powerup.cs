using UnityEngine;

public class Powerup : MonoBehaviour
{
    public enum PowerupType { Health, RapidFire, Shield, Grenade }
    public PowerupType type;

    [Header("Values")]
    public float healthAmount = 50f;
    public float rapidFireDuration = 5f;
    public float shieldDuration = 4f;
    public int grenadeAmount = 2;

    [Header("Effects")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;
    public float rotateSpeed = 90f;
    public ParticleSystem pickupEffect;
    public AudioClip pickupClip;

    private Vector3 startPos;
    private float bobTimer;

    void Start() { startPos = transform.position; }

    void Update()
    {
        bobTimer += Time.deltaTime * bobSpeed;
        transform.position = startPos + Vector3.up * Mathf.Sin(bobTimer) * bobHeight;
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        switch (type)
        {
            case PowerupType.Health:    player.PickupHealth(healthAmount); break;
            case PowerupType.RapidFire: player.PickupRapidFire(rapidFireDuration); break;
            case PowerupType.Shield:    player.PickupShield(shieldDuration); break;
            case PowerupType.Grenade:   player.PickupGrenade(grenadeAmount); break;
        }

        if (pickupEffect) { pickupEffect.transform.parent = null; pickupEffect.Play(); Destroy(pickupEffect.gameObject, 2f); }
        if (pickupClip) AudioSource.PlayClipAtPoint(pickupClip, transform.position);
        Destroy(gameObject);
    }
}
