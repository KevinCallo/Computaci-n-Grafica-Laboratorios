using UnityEngine;

public class ExitZone2D : MonoBehaviour
{
    [Header("Exit Settings")]
    public bool requiresAllCollectibles = true;
    public SpriteRenderer exitSprite;
    public Color lockedColor = new Color(0.8f, 0.3f, 0.3f);
    public Color openColor   = new Color(0.3f, 1f, 0.5f);

    private bool isOpen = false;

    void Start()
    {
        // Al inicio, configuramos el color según si requiere llaves o no
        if (exitSprite == null) exitSprite = GetComponent<SpriteRenderer>();
        
        if (exitSprite) exitSprite.color = requiresAllCollectibles ? lockedColor : openColor;
        isOpen = !requiresAllCollectibles;
    }

    void Update()
    {
        // Efecto visual: Pulso de escala constante
        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.08f;
        transform.localScale = Vector3.one * pulse;

        // Verificar si ya se recogieron todas las llaves
        if (requiresAllCollectibles && !isOpen && GameManager2D.Instance != null)
        {
            bool allFound = GameManager2D.Instance.collectiblesFound >= GameManager2D.Instance.totalCollectibles;
            if (allFound && GameManager2D.Instance.totalCollectibles > 0) 
            { 
                isOpen = true; 
                if (exitSprite) exitSprite.color = openColor; 
                GameManager2D.Instance.ShowMessage("Exit is now OPEN!", 2f);
            }
        }
    }

    public void TryExit()
    {
        if (!isOpen) 
        { 
            if (GameManager2D.Instance != null) {
                GameManager2D.Instance.ShowMessage("Collect all keys first!", 1.5f); 
                GameManager2D.Instance.PlayError(); 
            }
            return; 
        }

        // Si está abierto, avisamos al GameManager para pasar de nivel
        if (GameManager2D.Instance != null)
        {
            GameManager2D.Instance.SceneComplete();
        }
    }
}