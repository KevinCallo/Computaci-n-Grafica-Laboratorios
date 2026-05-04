using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Physics")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public float gravity = -20f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Transformation Controls")]
    public float rotationStep = 45f;        // degrees per Q/E press
    public float scaleDelta = 0.25f;        // scale change per Z/X press
    public float minScale = 0.3f;
    public float maxScale = 3f;
    public float translationStep = 1f;      // world units per arrow key
    public float selectionRange = 4f;

    [Header("Visual Feedback")]
    public SpriteRenderer sprite;
    public Color normalColor = Color.white;
    public Color holdingColor = new Color(0.4f, 0.9f, 1f);
    public GameObject selectionIndicatorPrefab;

    [Header("Effects")]
    public ParticleSystem jumpParticles;
    public ParticleSystem transformParticles;
    public AudioSource audioSource;
    public AudioClip jumpClip;
    public AudioClip transformClip;
    public AudioClip selectClip;
    public AudioClip errorClip;

    // State
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool facingRight = true;
    private TransformableObject selectedObject = null;
    private GameObject selectionIndicator;
    private float transformCooldown = 0f;
    private const float TRANSFORM_COOLDOWN = 0.15f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // we handle gravity manually for control
        if (selectionIndicatorPrefab)
            selectionIndicator = Instantiate(selectionIndicatorPrefab);
    }

    void Update()
    {
        if (!GameManager2D.Instance.gameActive) return;

        HandleMovement();
        HandleJump();
        HandleObjectSelection();
        HandleTransformations();

        if (transformCooldown > 0f) transformCooldown -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Apply manual gravity
        rb.linearVelocity += Vector2.up * gravity * Time.fixedDeltaTime;
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);

        // Flip sprite
        if (h > 0.1f && !facingRight) { facingRight = true; transform.localScale = new Vector3(1, 1, 1); }
        else if (h < -0.1f && facingRight) { facingRight = false; transform.localScale = new Vector3(-1, 1, 1); }

        // Update grounded
        isGrounded = groundCheck &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (jumpParticles) jumpParticles.Play();
            PlaySound(jumpClip);
        }
    }

    // ─────────────────────────────────────────────
    // OBJECT SELECTION  (click or Tab to cycle)
    // ─────────────────────────────────────────────
    void HandleObjectSelection()
    {
        // Mouse click to select
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit)
            {
                TransformableObject to = hit.GetComponent<TransformableObject>();
                if (to != null && to.isSelectable)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    if (dist <= selectionRange) { SelectObject(to); return; }
                    else { PlaySound(errorClip); GameManager2D.Instance.ShowMessage("Too far away!", 1f); }
                }
            }
            // Clicked empty space — deselect
            Deselect();
        }

        // Tab to cycle nearest selectable
        if (Input.GetKeyDown(KeyCode.Tab)) CycleSelection();

        // F to deselect / drop
        if (Input.GetKeyDown(KeyCode.F)) Deselect();

        // Update indicator position
        if (selectionIndicator)
        {
            selectionIndicator.SetActive(selectedObject != null);
            if (selectedObject) selectionIndicator.transform.position = selectedObject.transform.position;
        }
    }

    void SelectObject(TransformableObject to)
    {
        if (selectedObject) selectedObject.Deselect();
        selectedObject = to;
        selectedObject.Select();
        if (sprite) sprite.color = holdingColor;
        PlaySound(selectClip);
        GameManager2D.Instance.ShowMessage($"Selected: {to.objectName}\nQ/E=Rotate  Z/X=Scale  Arrows=Move", 2f);
    }

    void Deselect()
    {
        if (selectedObject) { selectedObject.Deselect(); selectedObject = null; }
        if (sprite) sprite.color = normalColor;
    }

    void CycleSelection()
    {
        TransformableObject[] all = FindObjectsByType<TransformableObject>(FindObjectsSortMode.None);
        TransformableObject nearest = null;
        float nearestDist = selectionRange;
        foreach (var t in all)
        {
            if (!t.isSelectable) continue;
            float d = Vector2.Distance(transform.position, t.transform.position);
            if (d < nearestDist && t != selectedObject) { nearestDist = d; nearest = t; }
        }
        if (nearest) SelectObject(nearest);
    }

    // ─────────────────────────────────────────────
    // GEOMETRIC TRANSFORMATIONS
    // ─────────────────────────────────────────────
    void HandleTransformations()
    {
        if (selectedObject == null || transformCooldown > 0f) return;

        bool acted = false;

        // ROTATION (Q = counter-clockwise, E = clockwise)
        if (selectedObject.canRotate)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                selectedObject.Rotate(rotationStep);
                acted = true;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                selectedObject.Rotate(-rotationStep);
                acted = true;
            }
        }

        // SCALE (Z = shrink, X = grow)
        if (selectedObject.canScale)
        {
            if (Input.GetKey(KeyCode.Z))
            {
                selectedObject.ScaleBy(-scaleDelta, minScale, maxScale);
                acted = true;
            }
            else if (Input.GetKey(KeyCode.X))
            {
                selectedObject.ScaleBy(scaleDelta, minScale, maxScale);
                acted = true;
            }
        }

        // TRANSLATION (Arrow keys move selected object)
        if (selectedObject.canTranslate)
        {
            Vector2 moveDir = Vector2.zero;
            if (Input.GetKey(KeyCode.UpArrow))    moveDir.y += translationStep;
            if (Input.GetKey(KeyCode.DownArrow))  moveDir.y -= translationStep;
            if (Input.GetKey(KeyCode.LeftArrow))  moveDir.x -= translationStep;
            if (Input.GetKey(KeyCode.RightArrow)) moveDir.x += translationStep;

            if (moveDir != Vector2.zero)
            {
                selectedObject.Translate(moveDir);
                acted = true;
            }
        }

        if (acted)
        {
            transformCooldown = TRANSFORM_COOLDOWN;
            PlaySound(transformClip);
            if (transformParticles) transformParticles.Play();
        }
    }

    void PlaySound(AudioClip clip) { if (audioSource && clip) audioSource.PlayOneShot(clip); }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Detectar Llaves
        Collectible2D col = other.GetComponent<Collectible2D>();
        if (col != null) 
        {
            col.Collect();
            return; // Salimos para evitar conflictos en el mismo frame
        }

        // Detectar Salida
        ExitZone2D exit = other.GetComponent<ExitZone2D>();
        if (exit != null) 
        {
            exit.TryExit();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, selectionRange);
        if (groundCheck) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius); }
    }
}
