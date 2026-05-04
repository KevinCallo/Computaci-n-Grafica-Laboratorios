using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Run via: GameObject menu > ShooterGame > Build Scene
/// Builds the full Combat Zone shooting game scene from scratch.
/// </summary>
public static class SceneBuilder
{
#if UNITY_EDITOR
    [MenuItem("GameObject/ShooterGame/Build Full Scene")]
    public static void BuildScene()
    {
        // Clear existing (keep camera/light)
        ClearScene();

        // --- ENVIRONMENT ---
        BuildArena();

        // --- PLAYER ---
        GameObject player = BuildPlayer();

        // --- SPAWN POINTS ---
        GameObject[] spawnPoints = BuildSpawnPoints();

        // --- ENEMY PREFABS (primitive placeholders) ---
        GameObject normalEnemy = BuildEnemyPrefab("NormalEnemy", Color.red, EnemyType.Normal, 40f, 100, 4f, 2.5f);
        GameObject fastEnemy   = BuildEnemyPrefab("FastEnemy",   new Color(1f,0.5f,0f), EnemyType.Fast, 25f, 75, 7f, 1.5f);
        GameObject tankEnemy   = BuildEnemyPrefab("TankEnemy",   new Color(0.5f,0f,0.5f), EnemyType.Tank, 100f, 150, 2.5f, 3f, true);

        // --- POWERUP PREFABS ---
        GameObject puHealth    = BuildPowerupPrefab("PU_Health",    Color.green,  Powerup.PowerupType.Health);
        GameObject puRapidFire = BuildPowerupPrefab("PU_RapidFire", Color.yellow, Powerup.PowerupType.RapidFire);
        GameObject puShield    = BuildPowerupPrefab("PU_Shield",    Color.cyan,   Powerup.PowerupType.Shield);

        // --- BULLET PREFABS ---
        GameObject bullet       = BuildBulletPrefab("Bullet",       Color.yellow, 0.08f, false);
        GameObject sniperBullet = BuildBulletPrefab("SniperBullet", Color.cyan,   0.05f, true);
        GameObject grenade      = BuildGrenadePrefab();
        GameObject enemyBullet  = BuildBulletPrefab("EnemyBullet",  Color.red,    0.1f,  false);

        // --- UI ---
        GameObject ui = BuildUI();

        // --- GAME MANAGER ---
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm = gmGO.AddComponent<GameManager>();

        // Wire spawn points
        gm.spawnPoints = new Transform[spawnPoints.Length];
        for (int i = 0; i < spawnPoints.Length; i++) gm.spawnPoints[i] = spawnPoints[i].transform;

        // Wire prefabs
        gm.normalEnemyPrefab    = normalEnemy;
        gm.fastEnemyPrefab      = fastEnemy;
        gm.tankEnemyPrefab      = tankEnemy;
        gm.powerupHealthPrefab  = puHealth;
        gm.powerupRapidFirePrefab = puRapidFire;
        gm.powerupShieldPrefab  = puShield;

        // Wire UI (find by name in canvas)
        Canvas canvas = ui.GetComponentInChildren<Canvas>();
        if (canvas)
        {
            gm.scoreText    = FindTMP(canvas, "ScoreText");
            gm.waveText     = FindTMP(canvas, "WaveText");
            gm.healthText   = FindTMP(canvas, "HealthText");
            gm.ammoText     = FindTMP(canvas, "AmmoText");
            gm.messageText  = FindTMP(canvas, "MessageText");
            gm.killFeedText = FindTMP(canvas, "KillFeedText");
            gm.startPanel   = FindChild(canvas.gameObject, "StartPanel");
            gm.gameOverPanel= FindChild(canvas.gameObject, "GameOverPanel");
            gm.finalScoreText = FindTMP(canvas, "FinalScoreText");
            gm.healthSlider = canvas.GetComponentInChildren<Slider>();
        }

        // Wire player weapons
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc)
        {
            pc.bulletPrefab       = bullet;
            pc.sniperBulletPrefab = sniperBullet;
            pc.grenadePrefab      = grenade;
        }

        // Wire enemy bullet
        foreach (var ep in new[] { normalEnemy, fastEnemy, tankEnemy })
        {
            EnemyController ec = ep.GetComponent<EnemyController>();
            if (ec) ec.enemyBulletPrefab = enemyBullet;
        }

        // Convert enemies to prefabs
        string prefabPath = "Assets/Prefabs/";
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Prefabs");
        AssetDatabase.Refresh();

        SavePrefab(normalEnemy, prefabPath + "NormalEnemy.prefab");
        SavePrefab(fastEnemy,   prefabPath + "FastEnemy.prefab");
        SavePrefab(tankEnemy,   prefabPath + "TankEnemy.prefab");
        SavePrefab(puHealth,    prefabPath + "PU_Health.prefab");
        SavePrefab(puRapidFire, prefabPath + "PU_RapidFire.prefab");
        SavePrefab(puShield,    prefabPath + "PU_Shield.prefab");
        SavePrefab(bullet,      prefabPath + "Bullet.prefab");
        SavePrefab(sniperBullet,prefabPath + "SniperBullet.prefab");
        SavePrefab(grenade,     prefabPath + "Grenade.prefab");
        SavePrefab(enemyBullet, prefabPath + "EnemyBullet.prefab");

        // Remove scene copies of prefabs (they were only needed to save to disk)
        GameObject.DestroyImmediate(normalEnemy);
        GameObject.DestroyImmediate(fastEnemy);
        GameObject.DestroyImmediate(tankEnemy);
        GameObject.DestroyImmediate(puHealth);
        GameObject.DestroyImmediate(puRapidFire);
        GameObject.DestroyImmediate(puShield);
        GameObject.DestroyImmediate(bullet);
        GameObject.DestroyImmediate(sniperBullet);
        GameObject.DestroyImmediate(grenade);
        GameObject.DestroyImmediate(enemyBullet);

        // Re-load prefabs from disk and wire
        gm.normalEnemyPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "NormalEnemy.prefab");
        gm.fastEnemyPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "FastEnemy.prefab");
        gm.tankEnemyPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "TankEnemy.prefab");
        gm.powerupHealthPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "PU_Health.prefab");
        gm.powerupRapidFirePrefab= AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "PU_RapidFire.prefab");
        gm.powerupShieldPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "PU_Shield.prefab");

        pc.bulletPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "Bullet.prefab");
        pc.sniperBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "SniperBullet.prefab");
        pc.grenadePrefab      = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "Grenade.prefab");

        foreach (var ep in new[] { gm.normalEnemyPrefab, gm.fastEnemyPrefab, gm.tankEnemyPrefab })
        {
            if (ep == null) continue;
            EnemyController ec = ep.GetComponent<EnemyController>();
            if (ec) { ec.enemyBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "EnemyBullet.prefab"); EditorUtility.SetDirty(ep); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ShooterGame] Scene built! Press Play to start. WASD=move, Mouse=aim, Click=shoot, Q=sniper, E=grenade, R=reload.");
    }

    static void ClearScene()
    {
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            string n = go.name;
            if (n == "Main Camera" || n == "Directional Light" || n == "Global Volume") continue;
            if (go.transform.parent == null) toDelete.Add(go);
        }
        foreach (var go in toDelete) Object.DestroyImmediate(go);
    }

    static void BuildArena()
    {
        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(5f, 1f, 5f);
        floor.GetComponent<Renderer>().material.color = new Color(0.15f, 0.18f, 0.2f);

        // Outer walls
        CreateWall("WallN", new Vector3(0, 2, 25),  new Vector3(50, 4, 1));
        CreateWall("WallS", new Vector3(0, 2, -25), new Vector3(50, 4, 1));
        CreateWall("WallE", new Vector3(25, 2, 0),  new Vector3(1, 4, 50));
        CreateWall("WallW", new Vector3(-25, 2, 0), new Vector3(1, 4, 50));

        // Cover obstacles
        CreateObstacle("Crate1",  new Vector3(8, 0.75f, 8),   new Vector3(2, 1.5f, 2), new Color(0.4f, 0.3f, 0.2f));
        CreateObstacle("Crate2",  new Vector3(-8, 0.75f, 8),  new Vector3(2, 1.5f, 2), new Color(0.4f, 0.3f, 0.2f));
        CreateObstacle("Crate3",  new Vector3(8, 0.75f, -8),  new Vector3(2, 1.5f, 2), new Color(0.4f, 0.3f, 0.2f));
        CreateObstacle("Crate4",  new Vector3(-8, 0.75f, -8), new Vector3(2, 1.5f, 2), new Color(0.4f, 0.3f, 0.2f));
        CreateObstacle("Pillar1", new Vector3(0, 1.5f, 12),   new Vector3(1.5f, 3f, 1.5f), new Color(0.3f, 0.3f, 0.35f));
        CreateObstacle("Pillar2", new Vector3(0, 1.5f, -12),  new Vector3(1.5f, 3f, 1.5f), new Color(0.3f, 0.3f, 0.35f));
        CreateObstacle("Barrier1",new Vector3(15, 0.5f, 0),   new Vector3(4, 1f, 1), new Color(0.5f, 0.4f, 0.3f));
        CreateObstacle("Barrier2",new Vector3(-15, 0.5f, 0),  new Vector3(4, 1f, 1), new Color(0.5f, 0.4f, 0.3f));

        // Ambient lights
        GameObject lightGO = new GameObject("AmbientLight");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.9f, 0.85f, 0.7f);
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position = pos;
        w.transform.localScale = scale;
        w.GetComponent<Renderer>().material.color = new Color(0.2f, 0.22f, 0.25f);
        w.tag = "Wall";
    }

    static void CreateObstacle(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name = name;
        o.transform.position = pos;
        o.transform.localScale = scale;
        o.GetComponent<Renderer>().material.color = color;
    }

    static GameObject BuildPlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1f, 0);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);

        PlayerController pc = player.AddComponent<PlayerController>();
        player.AddComponent<AudioSource>();
        pc.audioSource = player.GetComponent<AudioSource>();

        // Camera rig
        GameObject camRig = new GameObject("CameraRig");
        camRig.transform.parent = player.transform;
        camRig.transform.localPosition = new Vector3(0, 1.6f, 0);

        Camera cam = camRig.AddComponent<Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.1f;
        camRig.tag = "MainCamera";
        pc.firePoint = camRig.transform;
        pc.grenadePoint = camRig.transform;

        // Player body visual
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "PlayerBody";
        body.transform.parent = player.transform;
        body.transform.localPosition = new Vector3(0, 0.9f, 0);
        body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
        body.GetComponent<Renderer>().material.color = new Color(0.1f, 0.4f, 0.1f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        return player;
    }

    static GameObject[] BuildSpawnPoints()
    {
        Vector3[] positions = {
            new Vector3(23, 0, 0), new Vector3(-23, 0, 0),
            new Vector3(0, 0, 23), new Vector3(0, 0, -23),
            new Vector3(18, 0, 18), new Vector3(-18, 0, 18),
            new Vector3(18, 0, -18), new Vector3(-18, 0, -18)
        };
        GameObject[] sps = new GameObject[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            sps[i] = new GameObject("SpawnPoint_" + i);
            sps[i].transform.position = positions[i];
        }
        return sps;
    }

    static GameObject BuildEnemyPrefab(string name, Color color, EnemyType type, float hp, int score, float speed, float fireRate, bool isTank = false)
    {
        GameObject go = isTank ?
            GameObject.CreatePrimitive(PrimitiveType.Cube) :
            (type == EnemyType.Fast ? GameObject.CreatePrimitive(PrimitiveType.Capsule) : GameObject.CreatePrimitive(PrimitiveType.Sphere));
        go.name = name;
        go.GetComponent<Renderer>().material.color = color;
        go.transform.localScale = isTank ? new Vector3(1.4f, 1.4f, 1.4f) : Vector3.one;

        // Remove default collider, add CharacterController
        Object.DestroyImmediate(go.GetComponent<Collider>());
        CharacterController cc = go.AddComponent<CharacterController>();
        cc.height = isTank ? 1.4f : 1.8f;
        cc.radius = isTank ? 0.7f : 0.5f;
        cc.center = new Vector3(0, cc.height / 2f, 0);

        // Headshot point
        GameObject headshotGO = new GameObject("HeadshotPoint");
        headshotGO.transform.parent = go.transform;
        headshotGO.transform.localPosition = new Vector3(0, isTank ? 0.9f : 1.1f, 0);
        SphereCollider hsc = headshotGO.AddComponent<SphereCollider>();
        hsc.radius = 0.4f; hsc.isTrigger = true;

        // Fire point
        GameObject fpGO = new GameObject("FirePoint");
        fpGO.transform.parent = go.transform;
        fpGO.transform.localPosition = new Vector3(0, 0.8f, 0.6f);

        EnemyController ec = go.AddComponent<EnemyController>();
        ec.enemyType = type;
        ec.maxHealth = hp;
        ec.scoreValue = score;
        ec.moveSpeed = speed;
        ec.fireRate = fireRate;
        ec.headshotPoint = headshotGO.transform;
        ec.firePoint = fpGO.transform;
        go.AddComponent<AudioSource>();
        ec.audioSource = go.GetComponent<AudioSource>();

        go.tag = "Enemy";
        return go;
    }

    static GameObject BuildPowerupPrefab(string name, Color color, Powerup.PowerupType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.GetComponent<Renderer>().material.color = color;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Collider col = go.GetComponent<Collider>();
        if (col) col.isTrigger = true;
        Powerup pu = go.AddComponent<Powerup>();
        pu.type = type;
        return go;
    }

    static GameObject BuildBulletPrefab(string name, Color color, float radius, bool piercing)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.GetComponent<Renderer>().material.color = color;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        SphereCollider sc = go.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        Projectile proj = go.AddComponent<Projectile>();
        proj.isPiercing = piercing;
        proj.isHeadshotCapable = true;

        go.tag = "Projectile";
        return go;
    }

    static GameObject BuildGrenadePrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Grenade";
        go.transform.localScale = Vector3.one * 0.25f;
        go.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.1f);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        go.AddComponent<GrenadeProjectile>();
        go.AddComponent<AudioSource>();
        return go;
    }

    static GameObject BuildUI()
    {
        GameObject uiRoot = new GameObject("UI");
        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        uiRoot.AddComponent<GraphicRaycaster>();

        // HUD panel
        GameObject hud = CreatePanel(uiRoot, "HUD", new Vector2(0, 0), new Vector2(1, 1), Color.clear);

        // Score
        CreateTMP(hud, "ScoreText", new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, 60), new Vector2(10, -10), "SCORE\n0", 16, Color.green);
        // Wave
        CreateTMP(hud, "WaveText", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(120, 60), new Vector2(-60, -10), "WAVE\n1", 16, Color.white);
        // Ammo
        CreateTMP(hud, "AmmoText", new Vector2(1, 0), new Vector2(1, 0), new Vector2(180, 50), new Vector2(-100, 60), "12/12", 16, Color.yellow);
        // Health text
        CreateTMP(hud, "HealthText", new Vector2(0, 0), new Vector2(0, 0), new Vector2(100, 30), new Vector2(10, 50), "100 HP", 14, Color.green);
        // Kill feed
        CreateTMP(hud, "KillFeedText", new Vector2(1, 1), new Vector2(1, 1), new Vector2(220, 40), new Vector2(-120, -80), "", 13, new Color(1f, 0.5f, 0f));
        // Message center
        CreateTMP(hud, "MessageText", new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(400, 60), new Vector2(-200, 0), "", 24, Color.yellow);

        // Health slider
        GameObject sliderGO = new GameObject("HealthSlider");
        sliderGO.transform.SetParent(hud.transform, false);
        Slider sl = sliderGO.AddComponent<Slider>();
        RectTransform slrt = sliderGO.GetComponent<RectTransform>();
        slrt.anchorMin = new Vector2(0, 0); slrt.anchorMax = new Vector2(0, 0);
        slrt.sizeDelta = new Vector2(200, 18); slrt.anchoredPosition = new Vector2(10, 30);
        sl.value = 1f;

        GameObject slBg = CreatePanel(sliderGO, "Background", Vector2.zero, Vector2.one, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        GameObject slFill = CreatePanel(sliderGO, "Fill", Vector2.zero, Vector2.one, Color.green);
        GameObject fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.sizeDelta = Vector2.zero;
        slFill.transform.SetParent(fillArea.transform, false);
        sl.fillRect = slFill.GetComponent<RectTransform>();
        sl.targetGraphic = slFill.GetComponent<Image>();

        // Start Panel
        GameObject startPanel = CreatePanel(uiRoot, "StartPanel", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.85f));
        CreateTMP(startPanel, "Title", new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(500, 80), new Vector2(-250, 0), "COMBAT ZONE", 36, Color.green);
        CreateTMP(startPanel, "Sub",   new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 40), new Vector2(-200, 0), "WASD=Move | Mouse=Aim | Click=Shoot | Q=Sniper | E=Grenade | R=Reload", 11, Color.gray);
        GameObject startBtn = CreateButton(startPanel, "StartBtn", new Vector2(0.5f, 0.4f), "START MISSION", "startGame");

        // Game Over Panel
        GameObject gameOverPanel = CreatePanel(uiRoot, "GameOverPanel", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.9f));
        gameOverPanel.SetActive(false);
        CreateTMP(gameOverPanel, "GOTitle",    new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(400, 60), new Vector2(-200, 0), "MISSION FAILED", 32, Color.red);
        CreateTMP(gameOverPanel, "FinalScoreText", new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(300, 60), new Vector2(-150, 0), "SCORE: 0", 20, Color.white);
        CreateButton(gameOverPanel, "RetryBtn", new Vector2(0.5f, 0.42f), "RETRY", "startGame");

        return uiRoot;
    }

    static GameObject CreatePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static TextMeshProUGUI CreateTMP(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos, string text, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }

    static GameObject CreateButton(GameObject parent, string name, Vector2 anchor, string label, string method)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(200, 50); rt.anchoredPosition = new Vector2(-100, 0);
        Image img = go.AddComponent<Image>(); img.color = new Color(0.1f, 0.5f, 0.1f);
        Button btn = go.AddComponent<Button>();
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm) btn.onClick.AddListener(gm.StartGame);
        CreateTMP(go, "Label", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, label, 16, Color.white).alignment = TextAlignmentOptions.Center;
        return go;
    }

    static TextMeshProUGUI FindTMP(Canvas canvas, string name)
    {
        foreach (var t in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name == name) return t;
        return null;
    }

    static GameObject FindChild(GameObject root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
    }
#endif
}
