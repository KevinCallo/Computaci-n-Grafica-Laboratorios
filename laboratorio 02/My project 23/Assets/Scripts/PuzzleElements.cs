using UnityEngine;
using System.Collections;

// Las librerías de Editor deben estar al principio, incluso si están bajo una directiva #if
#if UNITY_EDITOR
using UnityEditor;
#endif

// ═══════════════════════════════════════════════════════
// COLLECTIBLE (KEY / STAR)
// ═══════════════════════════════════════════════════════
public class Collectible2D : MonoBehaviour
{
    [Header("Collectible")]
    public string collectibleName = "Key";
    public float bobSpeed    = 2f;
    public float bobHeight   = 0.2f;
    public float rotateSpeed = 180f;     // degrees/sec — demuestra rotación continua
    public float collectScaleTime = 0.3f;

    [Header("Effects")]
    public ParticleSystem collectEffect;
    public AudioClip collectClip;

    private Vector3 startPos;
    private bool collected = false;

    void Start() { startPos = transform.position; }

    void Update()
    {
        if (collected) return;
        // Bob (traslación) + Spin (rotación) — transformaciones visuales
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        if (collectEffect) { collectEffect.transform.parent = null; collectEffect.Play(); Destroy(collectEffect.gameObject, 2f); }
        if (collectClip) AudioSource.PlayClipAtPoint(collectClip, transform.position);
        GameManager2D.Instance.CollectibleFound();
        StartCoroutine(CollectAnimation());
    }

    IEnumerator CollectAnimation()
    {
        // Escalar y desaparecer — demuestra transformación de escala como feedback
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < collectScaleTime)
        {
            t += Time.deltaTime;
            float ratio = t / collectScaleTime;
            transform.localScale = startScale * (1f + ratio * 0.5f);
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 0f, 1f - ratio);
            yield return null;
        }
        Destroy(gameObject);
    }
}
// ═══════════════════════════════════════════════════════
// LEVER — activa transformaciones en objetos enlazados
// ═══════════════════════════════════════════════════════
public class Lever : MonoBehaviour
{
    [Header("Lever")]
    public TransformableObject[] targets;
    public LeverAction action = LeverAction.Rotate90;
    public bool toggle = true;
    public float interactRange = 2f;

    public enum LeverAction { Rotate90, Rotate180, ScaleUp, ScaleDown, TranslateUp, TranslateRight, OpenDoor }

    [Header("Linked Door")]
    public SlidingDoor linkedDoor;

    [Header("Visual")]
    public Sprite leverOnSprite;
    public Sprite leverOffSprite;
    private SpriteRenderer sr;
    private bool isOn = false;
    private float cooldown = 0f;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (cooldown > 0f) { cooldown -= Time.deltaTime; return; }

        if (!Input.GetKeyDown(KeyCode.F)) return;
        PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (dist > interactRange) return;

        Activate();
    }

    void Activate()
    {
        isOn = toggle ? !isOn : true;
        if (sr) sr.sprite = isOn ? leverOnSprite : leverOffSprite;
        cooldown = 0.5f;

        foreach (var t in targets)
        {
            if (t == null) continue;
            switch (action)
            {
                case LeverAction.Rotate90:       t.Rotate(isOn ? 90f : -90f); break;
                case LeverAction.Rotate180:      t.Rotate(isOn ? 180f : -180f); break;
                case LeverAction.ScaleUp:        t.ScaleBy(isOn ? 0.5f : -0.5f, 0.2f, 4f); break;
                case LeverAction.ScaleDown:      t.ScaleBy(isOn ? -0.5f : 0.5f, 0.2f, 4f); break;
                case LeverAction.TranslateUp:    t.Translate(isOn ? Vector2.up * 2f : Vector2.down * 2f); break;
                case LeverAction.TranslateRight: t.Translate(isOn ? Vector2.right * 2f : Vector2.left * 2f); break;
            }
        }

        if (action == LeverAction.OpenDoor && linkedDoor != null)
            linkedDoor.Open();

        GameManager2D.Instance.ShowMessage("Lever activated!", 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}


// ═══════════════════════════════════════════════════════
// MIRROR OBJECT — refleja/voltea objetos (escala -1)
// ═══════════════════════════════════════════════════════
public class MirrorObject : TransformableObject
{
    [Header("Mirror")]
    public MirrorAxis axis = MirrorAxis.Vertical;
    public bool reflectOnActivate = true;
    public GameObject[] objectsToReflect;

    public enum MirrorAxis { Horizontal, Vertical, Both }

    private bool reflected = false;

    protected override void Awake()
    {
        canRotate    = false;
        canScale     = false;
        canTranslate = false;
        base.Awake();
    }

    public void ActivateMirror()
    {
        reflected = !reflected;
        foreach (var go in objectsToReflect)
        {
            if (go == null) continue;
            Vector3 s = go.transform.localScale;
            switch (axis)
            {
                case MirrorAxis.Horizontal: s.y *= -1f; break;
                case MirrorAxis.Vertical:   s.x *= -1f; break;
                case MirrorAxis.Both:       s.x *= -1f; s.y *= -1f; break;
            }
            go.transform.localScale = s;
        }
        GameManager2D.Instance.ShowMessage($"Mirror reflected! ({axis} axis)", 1.5f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && reflectOnActivate) ActivateMirror();
    }
}

// ═══════════════════════════════════════════════════════
// SCENE BUILDER EDITOR TOOL
// ═══════════════════════════════════════════════════════
#if UNITY_EDITOR

public static class PuzzleSceneBuilder
{
    [MenuItem("GameObject/PuzzleGame2D/Build Epic Long Journey")]
    public static void BuildScenes()
    {
        // 1. Limpieza total
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.transform.parent == null && go.name != "Main Camera")
                Object.DestroyImmediate(go);

        GameObject root = new GameObject("Epic_Long_Level");

        // ── 2. BIOMA 1: EL TEMPLO GIRATORIO (X: -25 a 15) ─────────────────────
        CreatePlatform(root, "EntryFloor", new Vector2(-20, -4), new Vector2(12, 1), new Color(0.3f, 0.2f, 0.2f));
        
        // El "Salto de Fe" con plataformas que rotan
        CreateRotatingPlatform(root, "Gear1", new Vector2(-10, -1), new Vector2(4, 0.5f), 0, 90, false);
        CreateRotatingPlatform(root, "Gear2", new Vector2(-4, 1), new Vector2(4, 0.5f), 45, -45, false);
        CreateRotatingPlatform(root, "Gear3", new Vector2(2, 3), new Vector2(4, 0.5f), 90, 0, false);
        
        // Peligro: Aspas giratorias debajo
        CreateSpinner(root, "Hazard1", new Vector2(-4, -2), 120);

        // ── 3. BIOMA 2: EL BOSQUE ESCALABLE (X: 15 a 55) ─────────────────────
        CreatePlatform(root, "ForestFloor", new Vector2(25, -4), new Vector2(20, 1), new Color(0.1f, 0.4f, 0.1f));
        
        // Puzle de escala: Un "Hongo" que debes agrandar para subir a un saliente
        CreateScalingBridge(root, "MushroomJump", new Vector2(18, -3), new Vector2(1, 0.5f), 4.5f);
        CreatePlatform(root, "HighLedge", new Vector2(18, 2), new Vector2(5, 0.5f), new Color(0.1f, 0.4f, 0.1f));

        // Obstáculo: Pared de espinas que debes ENCOGER para pasar
        CreateShrinkableBoulder(root, "Thorns", new Vector2(32, -3), 0.4f);
        
        // Puente colapsado que debes estirar
        CreateScalingBridge(root, "GiantRoot", new Vector2(45, -1), new Vector2(1.2f, 0.4f), 6f);

        // ── 4. BIOMA 3: EL ABISMO MÓVIL (X: 55 a 95) ─────────────────────────
        // No hay suelo aquí, solo plataformas móviles (Traslación)
        CreateMovingPlatform(root, "Lift1", new Vector2(60, -2), new Vector2(60, 4), 2.5f);
        CreateMovingPlatform(root, "Ferry1", new Vector2(70, 0), new Vector2(85, 0), 3f);
        
        // Gema difícil de alcanzar en el aire
        CreateCollectible(root, "SkyGem", new Vector2(77, 4));

        // ── 5. BIOMA 4: LA FORTALEZA FINAL (X: 95 a 130) ──────────────────────
        CreatePlatform(root, "FortressBase", new Vector2(110, -4), new Vector2(40, 1), new Color(0.2f, 0.2f, 0.3f));
        
        // Puerta triple cerrada con palanca lejana
        var door = CreatePlatform(root, "BigGate", new Vector2(120, -1.5f), new Vector2(0.5f, 4f), Color.red);
        var sd = door.AddComponent<SlidingDoor>();
        sd.openOffset = new Vector2(0, 5);

        // Palanca escondida en una torre
        CreatePlatform(root, "Tower", new Vector2(100, 3), new Vector2(4, 0.5f), Color.gray);
        CreateLever(root, new Vector2(100, 3.5f), sd);

        // ── 6. COLECCIONABLES (10 Gemas para ganar) ──────────────────────────
        for(int i=0; i<10; i++) {
            float xPos = -18 + (i * 14.5f);
            CreateCollectible(root, "Gem"+i, new Vector2(xPos, Random.Range(-1f, 3f)));
        }

        // ── 7. AMBIENTE Y CÁMARA ─────────────────────────────────────────────
        BuildParallaxLayers(root);
        BuildPlayer(-22, 0);
        
        // Meta final
        CreateExit(root, new Vector2(125, -2.5f));

        // Configuración de Cámara
        Camera.main.orthographicSize = 5f;
        var pc = new GameObject("CameraControl").AddComponent<ParallaxController>();
        pc.target = GameObject.FindWithTag("Player").transform;
        pc.cameraBoundsMin = new Vector2(-22, -6);
        pc.cameraBoundsMax = new Vector2(130, 15);

        // UI y Game Manager
        var gm = new GameObject("GameManager").AddComponent<GameManager2D>();
        gm.totalCollectibles = 10;
        BuildUI(gm);

        Debug.Log("¡Nivel ÉPICO construido! 10 gemas, 4 zonas de desafíos.");
    }

    // ── FACTORÍAS MEJORADAS ──────────────────────────────────
    static GameObject CreatePlatform(GameObject parent, string name, Vector2 pos, Vector2 size, Color color) {
        GameObject go = new GameObject(name);
        go.transform.parent = parent.transform;
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    static void CreateRotatingPlatform(GameObject parent, string name, Vector2 pos, Vector2 size, float start, float target, bool auto) {
        var go = CreatePlatform(parent, name, pos, size, new Color(0.8f, 0.5f, 0.2f));
        go.transform.rotation = Quaternion.Euler(0, 0, start);
        var rp = go.AddComponent<RotatingPlatform>();
        rp.targetAngle = target;
        rp.autoRotateSpeed = auto ? 20f : 0f;
        rp.isSelectable = true;
    }

    static void CreateScalingBridge(GameObject parent, string name, Vector2 pos, Vector2 size, float reqScale) {
        var go = CreatePlatform(parent, name, pos, size, new Color(0.2f, 0.6f, 0.9f));
        var sb = go.AddComponent<ScalingBridge>();
        sb.requiredScaleX = reqScale;
        sb.isSelectable = true;
    }

    static void CreateShrinkableBoulder(GameObject parent, string name, Vector2 pos, float scale) {
        var go = CreatePlatform(parent, name, pos, new Vector2(1.5f, 1.5f), new Color(0.4f, 0.4f, 0.4f));
        
        // CAMBIO: Ahora añadimos el script nuevo
        var po = go.AddComponent<PushableObject2D>();
        po.objectName = name;
        po.isSelectable = true;
    }

    static void CreateMovingPlatform(GameObject parent, string name, Vector2 a, Vector2 b, float speed) {
        var go = CreatePlatform(parent, name, a, new Vector2(3, 0.5f), Color.cyan);
        var tp = go.AddComponent<TranslatingPlatform>();
        tp.pointA = a; tp.pointB = b; tp.speed = speed;
    }

    static void CreateSpinner(GameObject parent, string name, Vector2 pos, float speed) {
        var go = CreatePlatform(parent, name, pos, new Vector2(0.3f, 4f), Color.red);
        var s = go.AddComponent<SpinningHazard>();
        s.rotationSpeed = speed;
    }

    static void CreateLever(GameObject parent, Vector2 pos, SlidingDoor door) {
        var go = CreatePlatform(parent, "Lever", pos, new Vector2(0.5f, 0.8f), Color.yellow);
        var lev = go.AddComponent<Lever>();
        lev.action = Lever.LeverAction.OpenDoor;
        lev.linkedDoor = door;
    }

    static void CreateCollectible(GameObject parent, string name, Vector2 pos) {
        var go = new GameObject(name);
        go.transform.parent = parent.transform;
        go.transform.position = pos;
        go.AddComponent<SpriteRenderer>().sprite = CreateSquareSprite();
        go.GetComponent<SpriteRenderer>().color = Color.yellow;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        go.AddComponent<Collectible2D>();
    }

    static void CreateExit(GameObject parent, Vector2 pos) {
        var go = CreatePlatform(parent, "Exit", pos, new Vector2(1.5f, 2f), Color.green);
        go.AddComponent<ExitZone2D>();
        go.GetComponent<BoxCollider2D>().isTrigger = true;
    }

    static void BuildParallaxLayers(GameObject parent) {
        // Nombres para tus 6 capas
        string[] layerNames = { "Fondo_Cielo", "Nubes_Lejanas", "Nubes_Medias", "Nubes_Rayos", "Detalle_Extra", "Capa_Frontal" };
        
        // Factores de movimiento (1.0 = estático, 0.1 = se mueve casi con el jugador)
        float[] factors = { 0.99f, 0.90f, 0.75f, 0.50f, 0.30f, 0.15f }; 
        
        // Velocidades de animación automática (negativos para que vayan a la izquierda)
        float[] autoSpeeds = { -0.01f, -0.03f, -0.05f, -0.08f, -0.12f, -0.2f };

        for (int i = 0; i < 6; i++) {
            var go = new GameObject(layerNames[i]);
            go.transform.parent = parent.transform;
            
            // Las separamos en el eje Z para que Unity no tenga conflictos de profundidad
            go.transform.position = new Vector3(50, 5, 20 + i); 
            
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(); // Se reemplazará manualmente por tus PNG
            sr.drawMode = SpriteDrawMode.Tiled;
            
            // Ancho de 500 para que cubra todo el nivel largo de 130 unidades
            sr.size = new Vector2(500, 25); 
            
            // El número más bajo (-100) va al fondo, el más alto (-50) más cerca del jugador
            sr.sortingOrder = -100 + (i * 10); 

            var pl = go.AddComponent<ParallaxLayer>();
            pl.parallaxFactorX = factors[i];
            pl.autoScrollX = autoSpeeds[i];
            pl.infiniteScrollX = true;
        }
    }

    static void BuildPlayer(float x, float y) {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = new Vector2(x, y);
        go.AddComponent<SpriteRenderer>().sprite = CreateSquareSprite();
        go.GetComponent<SpriteRenderer>().color = Color.white;
        go.GetComponent<SpriteRenderer>().sortingOrder = 10;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<PlayerController2D>();
        var gc = new GameObject("GC");
        gc.transform.parent = go.transform;
        gc.transform.localPosition = new Vector2(0, -0.6f);
        go.GetComponent<PlayerController2D>().groundCheck = gc.transform;
    }

    static void BuildUI(GameManager2D gm) {
        var canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        gm.sceneTitle = CreateLabel(canvas.gameObject, "Title", new Vector2(0, -40), new Vector2(0.5f, 1));
        gm.collectiblesText = CreateLabel(canvas.gameObject, "Score", new Vector2(-100, -40), new Vector2(1, 1));
        gm.messageText = CreateLabel(canvas.gameObject, "Msg", Vector2.zero, new Vector2(0.5f, 0.5f));
        gm.messageText.gameObject.SetActive(false);
    }

    static TMPro.TextMeshProUGUI CreateLabel(GameObject canvas, string name, Vector2 pos, Vector2 anchor) {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 100);
        var t = go.AddComponent<TMPro.TextMeshProUGUI>();
        t.fontSize = 30; t.alignment = TMPro.TextAlignmentOptions.Center;
        return t;
    }

    static Sprite CreateSquareSprite() {
        var tex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64);
    }
}
#endif