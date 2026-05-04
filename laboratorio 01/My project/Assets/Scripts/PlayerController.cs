using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float gravity = -20f;
    public float mouseSensitivity = 2f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Weapons")]
    public int maxAmmo = 12;
    public int currentAmmo;
    public float reloadTime = 1.5f;
    public bool isReloading = false;
    public int grenadeCount = 3;

    [Header("Projectile Prefabs")]
    public GameObject bulletPrefab;
    public GameObject sniperBulletPrefab;
    public GameObject grenadePrefab;

    [Header("Physics Values (Inspector-Tunable)")]
    public float bulletSpeed = 40f;
    public float sniperSpeed = 80f;
    public float grenadeSpeed = 20f;
    public float grenadeArcHeight = 8f;
    public float bulletDamage = 25f;
    public float sniperDamage = 80f;
    public float grenadeDamage = 60f;
    public float grenadeRadius = 6f;

    [Header("Fire Points")]
    public Transform firePoint;
    public Transform grenadePoint;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip fireClip;
    public AudioClip reloadClip;
    public AudioClip grenadeClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;

    [Header("Power-Up State")]
    public bool hasShield = false;
    public bool hasRapidFire = false;
    public float shieldTimer = 0f;
    public float rapidFireTimer = 0f;

    private CharacterController cc;
    private float verticalVelocity = 0f;
    private float cameraPitch = 0f;
    private Camera playerCamera;
    private float sniperCooldown = 0f;
    private bool isDead = false;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        ResetPlayer();
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    public void ResetPlayer()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        isReloading = false;
        grenadeCount = 3;
        hasShield = false;
        hasRapidFire = false;
        isDead = false;
        transform.position = Vector3.zero + Vector3.up * 1f;
    }

void Update()
    {
        // ESTA ES LA LÍNEA DE SEGURIDAD:
        // Si el GameManager aún no ha despertado, no hagas nada en este frame.
        if (GameManager.Instance == null) return;

        // 1. Control del ratón según el estado del juego
        if (!GameManager.Instance.gameActive || isDead) 
        {
            // Si el juego no ha empezado, liberamos el ratón
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return; // No ejecutamos el resto del código si no estamos jugando
        }
        else 
        {
            // Si estamos jugando, bloqueamos el ratón para apuntar
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 2. Ejecución de las mecánicas de juego
        HandleMouseLook();
        HandleMovement();
        HandleWeapons();
        HandlePowerupTimers();

        if (sniperCooldown > 0f) sniperCooldown -= Time.deltaTime;

        GameManager.Instance.UpdateUI();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -80f, 80f);
        if (playerCamera) playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;

        if (cc.isGrounded) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        cc.Move(move * moveSpeed * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void HandleWeapons()
    {
        // Normal fire
        if (Input.GetButtonDown("Fire1") && !isReloading)
        {
            if (currentAmmo > 0) FireBullet();
            else StartCoroutine(Reload());
        }

        // Sniper (Q)
        if (Input.GetKeyDown(KeyCode.Q) && sniperCooldown <= 0f)
        {
            FireSniper();
            sniperCooldown = 3f;
        }

        // Grenade (E)
        if (Input.GetKeyDown(KeyCode.E) && grenadeCount > 0)
        {
            ThrowGrenade();
        }

        // Reload (R)
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Transform fp = firePoint;
        // Raycast to get aim point
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 target = ray.GetPoint(100f);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            target = hit.point;

        Vector3 dir = (target - fp.position).normalized;

        GameObject b = Instantiate(bulletPrefab, fp.position, Quaternion.LookRotation(dir));
        Projectile proj = b.GetComponent<Projectile>();
        if (proj)
        {
            proj.speed = bulletSpeed;
            proj.damage = hasRapidFire ? bulletDamage * 1.5f : bulletDamage;
            proj.isPlayerProjectile = true;
            proj.isHeadshotCapable = true;
        }

        currentAmmo--;
        if (muzzleFlash) muzzleFlash.Play();
        PlaySound(fireClip);

        if (hasRapidFire)
        {
            // Extra bullet with slight spread
            Vector3 spread = dir + playerCamera.transform.right * Random.Range(-0.05f, 0.05f) + playerCamera.transform.up * Random.Range(-0.05f, 0.05f);
            GameObject b2 = Instantiate(bulletPrefab, fp.position, Quaternion.LookRotation(spread.normalized));
            Projectile proj2 = b2.GetComponent<Projectile>();
            if (proj2) { proj2.speed = bulletSpeed; proj2.damage = bulletDamage; proj2.isPlayerProjectile = true; proj2.isHeadshotCapable = true; }
        }

        if (currentAmmo <= 0) StartCoroutine(Reload());
    }

    void FireSniper()
    {
        if (sniperBulletPrefab == null || firePoint == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 target = ray.GetPoint(300f);
        if (Physics.Raycast(ray, out RaycastHit hit, 300f)) target = hit.point;

        Vector3 dir = (target - firePoint.position).normalized;
        GameObject b = Instantiate(sniperBulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        Projectile proj = b.GetComponent<Projectile>();
        if (proj)
        {
            proj.speed = sniperSpeed;
            proj.damage = sniperDamage;
            proj.isPlayerProjectile = true;
            proj.isPiercing = true;
            proj.isHeadshotCapable = true;
        }
        PlaySound(fireClip);
        if (muzzleFlash) muzzleFlash.Play();
        GameManager.Instance.ShowMessage("SNIPER READY IN 3s", 1f);
    }

    void ThrowGrenade()
    {
        if (grenadePrefab == null || grenadePoint == null) return;

        Vector3 throwDir = playerCamera.transform.forward + playerCamera.transform.up * 0.3f;
        GameObject g = Instantiate(grenadePrefab, grenadePoint.position, Quaternion.identity);
        GrenadeProjectile gp = g.GetComponent<GrenadeProjectile>();
        if (gp)
        {
            gp.throwDirection = throwDir.normalized;
            gp.speed = grenadeSpeed;
            gp.damage = grenadeDamage;
            gp.blastRadius = grenadeRadius;
        }
        grenadeCount--;
        PlaySound(grenadeClip);
    }

    IEnumerator Reload()
    {
        isReloading = true;
        PlaySound(reloadClip);
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    void HandlePowerupTimers()
    {
        if (hasShield) { shieldTimer -= Time.deltaTime; if (shieldTimer <= 0f) { hasShield = false; GameManager.Instance.ShowMessage("SHIELD EXPIRED", 1.5f); } }
        if (hasRapidFire) { rapidFireTimer -= Time.deltaTime; if (rapidFireTimer <= 0f) { hasRapidFire = false; GameManager.Instance.ShowMessage("RAPID FIRE EXPIRED", 1.5f); } }
    }

    public void TakeDamage(float dmg)
    {
        if (hasShield) { GameManager.Instance.ShowMessage("SHIELD BLOCKED!", 0.5f); return; }
        currentHealth -= dmg;
        PlaySound(hurtClip);
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isDead = true;
            PlaySound(deathClip);
            GameManager.Instance.PlayerDied();
        }
        GameManager.Instance.UpdateUI();
    }

    public void PickupHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        GameManager.Instance.ShowMessage("+HP RESTORED", 1.5f);
        GameManager.Instance.UpdateUI();
    }

    public void PickupRapidFire(float duration)
    {
        hasRapidFire = true; rapidFireTimer = duration;
        GameManager.Instance.ShowMessage("RAPID FIRE x2!", 1.5f);
    }

    public void PickupShield(float duration)
    {
        hasShield = true; shieldTimer = duration;
        GameManager.Instance.ShowMessage("SHIELD ACTIVE!", 1.5f);
    }

    public void PickupGrenade(int count)
    {
        grenadeCount = Mathf.Min(grenadeCount + count, 5);
        GameManager.Instance.ShowMessage("GRENADES +" + count, 1.5f);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    void OnGUI()
    {
        // Simple crosshair drawn in OnGUI as fallback if no texture
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        float size = 10f;
        GUI.color = hasShield ? Color.cyan : Color.green;
        GUI.DrawTexture(new Rect(cx - 1, cy - size, 2, size * 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - size, cy - 1, size * 2, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
