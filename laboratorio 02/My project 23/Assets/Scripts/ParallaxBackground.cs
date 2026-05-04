using UnityEngine;

/// <summary>
/// Attach to a background layer. Each layer scrolls at a different speed
/// relative to the camera, creating the parallax depth effect.
/// This is a geometric translation applied continuously based on camera movement.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    public float parallaxFactorX = 0.5f;  // 0 = moves with camera, 1 = fixed
    [Range(0f, 1f)]
    public float parallaxFactorY = 0f;    // usually 0 for horizontal parallax

    [Header("Infinite Scroll")]
    public bool infiniteScrollX = true;
    public bool infiniteScrollY = false;

    [Header("Auto Scroll")]
    public float autoScrollX = 0f;        // constant background drift
    public float autoScrollY = 0f;

    private Transform cam;
    private Vector3 lastCamPos;
    private float spriteWidth;
    private float spriteHeight;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            spriteWidth  = sr.bounds.size.x;
            spriteHeight = sr.bounds.size.y;
        }
    }

    void LateUpdate()
    {
        // Camera delta since last frame
        Vector3 delta = cam.position - lastCamPos;

        // PARALLAX TRANSLATION: move layer by fraction of camera movement
        // This is a direct application of the translation transformation T(dx, dy)
        float dx = delta.x * (1f - parallaxFactorX) + autoScrollX * Time.deltaTime;
        float dy = delta.y * (1f - parallaxFactorY) + autoScrollY * Time.deltaTime;

        transform.position += new Vector3(dx, dy, 0f);

        lastCamPos = cam.position;

        // Infinite scroll — teleport layer when it moves too far
        if (infiniteScrollX && spriteWidth > 0f)
        {
            float distX = cam.position.x - transform.position.x;
            if (Mathf.Abs(distX) >= spriteWidth)
                transform.position += new Vector3(Mathf.Sign(distX) * spriteWidth, 0f, 0f);
        }
        if (infiniteScrollY && spriteHeight > 0f)
        {
            float distY = cam.position.y - transform.position.y;
            if (Mathf.Abs(distY) >= spriteHeight)
                transform.position += new Vector3(0f, Mathf.Sign(distY) * spriteHeight, 0f);
        }
    }
}


/// <summary>
/// Manages all parallax layers in a scene and demonstrates
/// tile-based level construction.
/// </summary>
public class ParallaxController : MonoBehaviour
{
    [Header("Layers (back to front)")]
    public ParallaxLayer[] layers;

    [Header("Camera Follow")]
    public Transform target;           // player transform
    public float followSmoothness = 5f;
    public Vector2 deadZone = new Vector2(1f, 0.5f);
    public Vector2 cameraBoundsMin = new Vector2(-50f, -10f);
    public Vector2 cameraBoundsMax = new Vector2(50f, 10f);

    private Camera cam;
    private Vector3 targetPos;

    void Start()
    {
        cam = Camera.main;
        if (target) targetPos = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, cam.transform.position.z);

        // Dead zone: camera only moves when player exits a threshold region
        Vector3 diff = desired - cam.transform.position;
        if (Mathf.Abs(diff.x) < deadZone.x) desired.x = cam.transform.position.x;
        if (Mathf.Abs(diff.y) < deadZone.y) desired.y = cam.transform.position.y;

        // Clamp to bounds
        desired.x = Mathf.Clamp(desired.x, cameraBoundsMin.x, cameraBoundsMax.x);
        desired.y = Mathf.Clamp(desired.y, cameraBoundsMin.y, cameraBoundsMax.y);

        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, followSmoothness * Time.deltaTime);
    }
}


/// <summary>
/// Tile-based level element. Tiles snap to a grid (discrete translation).
/// The tile system demonstrates how translation is quantized in grid-based games.
/// </summary>
public class TileObject : TransformableObject
{
    [Header("Tile Grid")]
    public float gridSize = 1f;
    public bool snapToGrid = true;
    public Color tileColor = Color.white;

    [Header("Tile Type")]
    public TileType tileType = TileType.Solid;

    public enum TileType { Solid, PassThrough, Hazard, Ice, Bounce }

    private PhysicsMaterial2D iceMaterial;
    private PhysicsMaterial2D normalMaterial;

    protected override void Awake()
    {
        canTranslate = true;
        canRotate    = false;
        canScale     = false;
        base.Awake();
        if (sr) sr.color = GetTileColor();
        ApplyTileProperties();
    }

    public override void Translate(Vector2 delta)
    {
        if (snapToGrid)
        {
            // DISCRETE TRANSLATION — snap to grid
            Vector3 cur = targetPosition;
            cur.x = Mathf.Round((cur.x + delta.x) / gridSize) * gridSize;
            cur.y = Mathf.Round((cur.y + delta.y) / gridSize) * gridSize;
            // Set directly via base approach
            delta = new Vector2(cur.x - targetPosition.x, cur.y - targetPosition.y);
        }
        base.Translate(delta);
    }

    Color GetTileColor()
    {
        return tileType switch
        {
            TileType.Solid       => new Color(0.5f, 0.4f, 0.3f),
            TileType.PassThrough => new Color(0.6f, 0.8f, 0.6f, 0.7f),
            TileType.Hazard      => new Color(1f,   0.3f, 0.2f),
            TileType.Ice         => new Color(0.7f, 0.9f, 1f),
            TileType.Bounce      => new Color(1f,   0.8f, 0.2f),
            _                    => Color.white
        };
    }

    void ApplyTileProperties()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        switch (tileType)
        {
            case TileType.PassThrough:
            if (GetComponent<PlatformEffector2D>() != null) { } 
                break;
            case TileType.Hazard:
                col.isTrigger = true;
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (tileType != TileType.Hazard) return;
        PlayerController2D player = other.GetComponent<PlayerController2D>();
        if (player) GameManager2D.Instance.ShowMessage("Hazard! Respawning...", 1.5f);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (tileType != TileType.Bounce) return;
        Rigidbody2D rb = col.gameObject.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 15f);
    }
}
