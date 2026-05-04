using UnityEngine;

public class SpinningHazard : MonoBehaviour
{
    [Header("Spinning Settings")]
    public float rotationSpeed = 120f;
    public bool clockwise = true;

    [Header("Visual Pulse")]
    public bool pulseScale = false;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.2f;
    private Vector3 baseScale;

    void Start() 
    { 
        baseScale = transform.localScale; 
    }

    void Update()
    {
        // Rotación continua
        float dir = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, dir * rotationSpeed * Time.deltaTime);

        // Pulso de escala opcional
        if (pulseScale)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * s;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager2D.Instance != null)
                GameManager2D.Instance.ShowMessage("Watch out! Hazard hit!", 1.5f);
            
            // Aquí podrías añadir que el jugador respanee o pierda vida
        }
    }
}