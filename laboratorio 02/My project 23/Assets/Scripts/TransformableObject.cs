using UnityEngine;
using System.Collections;

/// <summary>
/// Base component for any puzzle object the player can transform.
/// Attach to platforms, blocks, doors, bridges, etc.
/// </summary>
public class TransformableObject : MonoBehaviour
{
    [Header("Identity")]
    public string objectName = "Block";
    public bool isSelectable = true;

    [Header("Allowed Transformations")]
    public bool canRotate    = true;
    public bool canScale     = true;
    public bool canTranslate = false; // most objects are anchored; enable per-object

    [Header("Rotation Constraints")]
    public bool snapRotation = true;        // snap to 45° increments
    public float snapAngle   = 45f;
    public float minAngle    = -360f;
    public float maxAngle    =  360f;
    public bool unlimitedRotation = true;

    [Header("Scale Constraints")]
    public Vector2 scaleDirection = Vector2.one; // (1,0)=X only, (0,1)=Y only, (1,1)=both

    [Header("Translation Constraints")]
    public bool constrainX = false;
    public bool constrainY = false;
    public Vector2 translationMin = new Vector2(-10, -10);
    public Vector2 translationMax = new Vector2(10, 10);

    [Header("Visual Feedback")]
    public Color normalColor   = Color.white;
    public Color selectedColor = new Color(1f, 0.9f, 0.2f);
    public Color lockedColor   = new Color(0.5f, 0.5f, 0.5f);
    public bool showTransformGhost = true;

    [Header("Animation")]
    public float transformSmoothSpeed = 8f;
    public bool animateTransforms = true;

    [Header("Effects")]
    public ParticleSystem transformEffect;
    public AudioSource audioSource;
    public AudioClip transformClip;

    // State
    protected SpriteRenderer sr;
    protected bool isSelected = false;
    protected Vector3 targetPosition;
    protected Quaternion targetRotation;
    protected Vector3 targetScale;
    protected Vector3 originalScale;
    protected float currentAngle = 0f;
    protected bool isLocked = false;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        targetScale    = transform.localScale;
    }

    protected virtual void Update()
    {
        if (animateTransforms)
        {
            float t = transformSmoothSpeed * Time.deltaTime;
            transform.position   = Vector3.Lerp(transform.position,   targetPosition, t);
            transform.rotation   = Quaternion.Lerp(transform.rotation, targetRotation, t);
            transform.localScale = Vector3.Lerp(transform.localScale,  targetScale,    t);
        }
    }

    // ─────────────────────────────────────────
    // GEOMETRIC TRANSFORMATION METHODS
    // ─────────────────────────────────────────

    /// <summary>ROTATION — rotates the object by deltaAngle degrees (Z axis in 2D)</summary>
    public virtual void Rotate(float deltaAngle)
    {
        if (!canRotate || isLocked) return;

        currentAngle += deltaAngle;
        if (!unlimitedRotation)
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

        float snapped = snapRotation ? SnapToGrid(currentAngle, snapAngle) : currentAngle;
        targetRotation = Quaternion.Euler(0f, 0f, snapped);
        PlayTransformEffect();
        OnTransformed("rotate");
    }

    /// <summary>SCALE — changes object size uniformly or per-axis</summary>
    public virtual void ScaleBy(float delta, float minS, float maxS)
    {
        if (!canScale || isLocked) return;

        Vector3 newScale = targetScale;
        newScale.x = Mathf.Clamp(newScale.x + delta * scaleDirection.x, minS * originalScale.x, maxS * originalScale.x);
        newScale.y = Mathf.Clamp(newScale.y + delta * scaleDirection.y, minS * originalScale.y, maxS * originalScale.y);
        targetScale = newScale;
        PlayTransformEffect();
        OnTransformed("scale");
    }

    /// <summary>TRANSLATION — moves the object in world space</summary>
    public virtual void Translate(Vector2 delta)
    {
        if (!canTranslate || isLocked) return;

        Vector3 newPos = targetPosition + new Vector3(
            constrainX ? 0f : delta.x,
            constrainY ? 0f : delta.y,
            0f);
        newPos.x = Mathf.Clamp(newPos.x, translationMin.x, translationMax.x);
        newPos.y = Mathf.Clamp(newPos.y, translationMin.y, translationMax.y);
        targetPosition = newPos;
        PlayTransformEffect();
        OnTransformed("translate");
    }

    /// <summary>Reset all transforms to original state</summary>
    public virtual void ResetTransforms()
    {
        currentAngle    = 0f;
        targetRotation  = Quaternion.identity;
        targetScale     = originalScale;
        targetPosition  = transform.position; // keep position
    }

    // ─────────────────────────────────────────
    // SELECTION
    // ─────────────────────────────────────────
    public virtual void Select()
    {
        isSelected = true;
        if (sr) sr.color = isLocked ? lockedColor : selectedColor;
        StartCoroutine(PulseEffect());
    }

    public virtual void Deselect()
    {
        isSelected = false;
        if (sr) sr.color = normalColor;
        StopCoroutine("PulseEffect");
    }

    public void Lock()   { isLocked = true;  if (sr) sr.color = lockedColor; }
    public void Unlock() { isLocked = false; if (sr) sr.color = isSelected ? selectedColor : normalColor; }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────
    float SnapToGrid(float angle, float grid)
    {
        return Mathf.Round(angle / grid) * grid;
    }

    void PlayTransformEffect()
    {
        if (transformEffect) transformEffect.Play();
        if (audioSource && transformClip) audioSource.PlayOneShot(transformClip);
    }

    IEnumerator PulseEffect()
    {
        while (isSelected)
        {
            float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            if (sr) sr.color = Color.Lerp(selectedColor, Color.white, t * 0.3f);
            yield return null;
        }
    }

    protected virtual void OnTransformed(string type) { }

    void OnDrawGizmosSelected()
    {
        if (!canTranslate) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector2 size = translationMax - translationMin;
        Gizmos.DrawWireCube(new Vector3((translationMin.x + translationMax.x) / 2f,
                                        (translationMin.y + translationMax.y) / 2f, 0),
                            new Vector3(size.x, size.y, 0));
    }
}
