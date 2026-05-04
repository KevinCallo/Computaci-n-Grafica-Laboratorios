using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════
// SCENE 1: ROTATING TEMPLE
// ═══════════════════════════════════════════════════════

/// <summary>
/// A platform the player can rotate to create walkable paths.
/// Used in Scene 1 (Rotating Temple).
/// </summary>
public class RotatingPlatform : TransformableObject
{
    [Header("Rotating Platform")]
    public float autoRotateSpeed = 0f;      // > 0 = platform rotates on its own
    public bool reverseOnSolve  = false;
    public float targetAngle    = 90f;      // angle that "solves" this puzzle piece
    public float solveThreshold = 5f;       // degrees tolerance

    [Header("Solve Feedback")]
    public SpriteRenderer lockIcon;
    public Color solvedColor = new Color(0.2f, 1f, 0.4f);
    public ParticleSystem solveEffect;

    private bool isSolved = false;
    private float autoAngle = 0f;

    protected override void Update()
    {
        base.Update();

        if (autoRotateSpeed != 0f && !isSolved)
        {
            autoAngle += autoRotateSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, autoAngle);
        }

        CheckSolved();
    }

    void CheckSolved()
    {
        if (isSolved) return;
        float angle = transform.eulerAngles.z;
        float diff  = Mathf.DeltaAngle(angle, targetAngle);
        if (Mathf.Abs(diff) < solveThreshold)
        {
            isSolved = true;
            if (sr) sr.color = solvedColor;
            if (solveEffect) solveEffect.Play();
            if (lockIcon) lockIcon.gameObject.SetActive(false);
            Lock(); // prevent further rotation
            GameManager2D.Instance.ShowMessage("Platform locked in place! ✓", 1.5f);
            OnSolved();
        }
    }

    protected virtual void OnSolved() { }

    protected override void OnTransformed(string type)
    {
        if (type == "rotate") CheckSolved();
    }
}

// ─────────────────────────────────────────────────────────

/// <summary>
/// A gear that rotates when the player interacts with a nearby lever.
/// Visually demonstrates rotation transformation.
/// </summary>
public class GearObject : MonoBehaviour
{
    [Header("Gear")]
    public float rotationSpeed = 90f;   // degrees per second
    public bool clockwise = true;
    public GearObject connectedGear;    // gears can drive each other
    public float gearRatio = -1f;       // -1 = opposite direction, 0.5 = half speed

    private bool isSpinning = false;

    public void StartSpin() { isSpinning = true; if (connectedGear) connectedGear.StartSpin(); }
    public void StopSpin()  { isSpinning = false; if (connectedGear) connectedGear.StopSpin(); }

    void Update()
    {
        if (!isSpinning) return;
        float dir = clockwise ? -1f : 1f;
        transform.Rotate(0, 0, dir * rotationSpeed * Time.deltaTime);
        if (connectedGear && connectedGear != this)
        {
            connectedGear.transform.Rotate(0, 0, -dir * connectedGear.rotationSpeed * gearRatio * Time.deltaTime);
        }
    }
}

// ─────────────────────────────────────────────────────────

/// <summary>
/// A key the player must rotate to the correct orientation to unlock a door.
/// </summary>
public class RotatingKey : TransformableObject
{
    [Header("Key")]
    public RotatingLock targetLock;
    public float correctAngle = 0f;
    public float tolerance = 10f;

    protected override void OnTransformed(string type)
    {
        if (type != "rotate" || targetLock == null) return;
        float diff = Mathf.DeltaAngle(transform.eulerAngles.z, correctAngle);
        if (Mathf.Abs(diff) < tolerance) targetLock.Unlock();
    }
}

public class RotatingLock : MonoBehaviour
{
    public GameObject door;
    public float openSpeed = 2f;

    public void Unlock()
    {
        StartCoroutine(OpenDoor());
        GameManager2D.Instance.ShowMessage("🔓 Lock opened!", 2f);
    }

    IEnumerator OpenDoor()
    {
        if (door == null) yield break;
        float elapsed = 0f;
        Vector3 start = door.transform.position;
        Vector3 end   = start + Vector3.down * 3f; // slide down
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            door.transform.position = Vector3.Lerp(start, end, elapsed);
            yield return null;
        }
        door.SetActive(false);
    }
}


// ═══════════════════════════════════════════════════════
// SCENE 2: SCALING CAVES
// ═══════════════════════════════════════════════════════

/// <summary>
/// A bridge block the player scales horizontally to span gaps.
/// </summary>
public class ScalingBridge : TransformableObject
{
    [Header("Scaling Bridge")]
    public float requiredScaleX = 2.5f;
    public float solveThreshold = 0.15f;
    public Color solvedColor = new Color(0.3f, 0.8f, 1f);
    public ParticleSystem solveEffect;

    private bool isSolved = false;

    protected override void Awake()
    {
        scaleDirection = new Vector2(1f, 0f); // only scale X
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
        CheckSolved();
    }

    void CheckSolved()
    {
        if (isSolved) return;
        if (Mathf.Abs(transform.localScale.x - requiredScaleX) < solveThreshold)
        {
            isSolved = true;
            if (sr) sr.color = solvedColor;
            if (solveEffect) solveEffect.Play();
            Lock();
            GameManager2D.Instance.ShowMessage("Bridge complete! ✓", 1.5f);
        }
    }

    protected override void OnTransformed(string type) { if (type == "scale") CheckSolved(); }
}
/// <summary>
/// A pressure plate activated when a scaled block is placed on it (block must be big enough).
/// </summary>
public class ScalePressurePlate : MonoBehaviour
{
    [Header("Pressure Plate")]
    public float minWeightScale = 1.5f;  // block's average scale must be >= this
    public GameObject[] activatedObjects; // doors/platforms to enable
    public Color activatedColor = Color.green;
    private SpriteRenderer sr;
    private bool activated = false;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (activated) return;
        TransformableObject to = col.gameObject.GetComponent<TransformableObject>();
        if (to != null)
        {
            float avgScale = (col.transform.localScale.x + col.transform.localScale.y) * 0.5f;
            if (avgScale >= minWeightScale) Activate();
        }
    }

    void Activate()
    {
        activated = true;
        if (sr) sr.color = activatedColor;
        foreach (var go in activatedObjects) if (go) go.SetActive(true);
        GameManager2D.Instance.ShowMessage("Pressure plate activated! ✓", 1.5f);
    }
}


// ═══════════════════════════════════════════════════════
// SCENE 3: PARALLAX FORTRESS
// ═══════════════════════════════════════════════════════

/// <summary>
/// Moving platform that translates back and forth.
/// Demonstrates translation transformation.
/// </summary>
public class TranslatingPlatform : TransformableObject
{
    [Header("Translating Platform")]
    public Vector2 pointA = new Vector2(-3f, 0f);
    public Vector2 pointB = new Vector2(3f, 0f);
    public float speed = 2f;
    public bool autoMove = true;
    public bool playerControlled = false; // if true, player moves it with arrow keys

    private Vector2 targetPoint;
    private bool goingToB = true;

    protected override void Awake()
    {
        canTranslate = playerControlled;
        canRotate    = false;
        canScale     = false;
        base.Awake();
        targetPoint = pointB;
    }

    protected override void Update()
    {
        if (playerControlled) { base.Update(); return; }
        if (!autoMove) return;

        // Auto translation between A and B
        Vector2 cur = transform.position;
        Vector2 target = goingToB ? (Vector2)transform.position + (pointB - pointA).normalized * speed * Time.deltaTime
                                  : (Vector2)transform.position + (pointA - pointB).normalized * speed * Time.deltaTime;

        transform.position = new Vector3(target.x, target.y, 0f);

        if (Vector2.Distance(transform.position, goingToB ? pointA + (pointB - pointA) : pointA) < 0.1f)
            goingToB = !goingToB;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawWireSphere(pointA, 0.15f);
        Gizmos.DrawWireSphere(pointB, 0.15f);
    }
}

// ─────────────────────────────────────────────────────────

/// <summary>
/// A door that slides open (translation) when all required keys are collected.
/// </summary>
public class SlidingDoor : MonoBehaviour
{
    [Header("Sliding Door")]
    public int keysRequired = 1;
    public Vector2 openOffset = new Vector2(0f, 3f);  // where it slides to
    public float openSpeed = 3f;
    public bool isOpen = false;
    public Color lockedColor = new Color(1f, 0.3f, 0.3f);
    public Color openColor   = new Color(0.3f, 1f, 0.3f);

    private SpriteRenderer sr;
    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isMoving = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        closedPos = transform.position;
        openPos   = closedPos + new Vector3(openOffset.x, openOffset.y, 0f);
        if (sr) sr.color = lockedColor;
    }

    public void NotifyKeyCollected(int totalKeys)
    {
        if (totalKeys >= keysRequired && !isOpen) Open();
    }

    public void Open()
    {
        isOpen = true;
        if (sr) sr.color = openColor;
        GameManager2D.Instance.ShowMessage("Door opening! ✓", 1.5f);
        StartCoroutine(SlideOpen());
    }

    IEnumerator SlideOpen()
    {
        isMoving = true;
        // Translation animation
        while (Vector3.Distance(transform.position, openPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, openPos, openSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = openPos;
        GetComponent<Collider2D>().enabled = false;
        isMoving = false;
    }
}
