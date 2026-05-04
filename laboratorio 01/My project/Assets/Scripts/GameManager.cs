using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int score = 0;
    public int wave = 1;
    public bool gameActive = false;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI killFeedText;
    public Slider healthSlider;
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    [Header("Wave Settings")]
    public float timeBetweenWaves = 5f;
    public int enemiesPerWave = 5;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Prefabs")]
    public GameObject normalEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject powerupHealthPrefab;
    public GameObject powerupRapidFirePrefab;
    public GameObject powerupShieldPrefab;

    private int enemiesAlive = 0;
    private int enemiesSpawnedThisWave = 0;
    private Coroutine waveCoroutine;
    private float killFeedTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ShowStartPanel();
    }

    void Update()
    {
        if (killFeedTimer > 0f)
        {
            killFeedTimer -= Time.deltaTime;
            if (killFeedTimer <= 0f && killFeedText != null)
                killFeedText.text = "";
        }
    }

    public void StartGame()
    {
        score = 0;
        wave = 1;
        gameActive = true;
        enemiesAlive = 0;
        enemiesSpawnedThisWave = 0;

        if (startPanel) startPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player) player.ResetPlayer();

        UpdateUI();
        StartCoroutine(StartWave());
    }

    IEnumerator StartWave()
    {
        ShowMessage("WAVE " + wave, 2f);
        yield return new WaitForSeconds(2f);

        enemiesAlive = 0;
        enemiesSpawnedThisWave = 0;
        int totalEnemies = enemiesPerWave + (wave - 1) * 3;

        while (enemiesSpawnedThisWave < totalEnemies)
        {
            SpawnEnemy();
            enemiesSpawnedThisWave++;
            yield return new WaitForSeconds(Mathf.Max(0.5f, 1.5f - wave * 0.1f));
        }

        // Wait until all enemies dead
        while (enemiesAlive > 0)
            yield return new WaitForSeconds(0.5f);

        if (!gameActive) yield break;

        // Powerup between waves
        SpawnPowerup();

        ShowMessage("WAVE COMPLETE!", 2f);
        yield return new WaitForSeconds(timeBetweenWaves);

        wave++;
        UpdateUI();
        StartCoroutine(StartWave());
    }

    void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject prefab;
        float r = Random.value;
        if (wave >= 3 && r < 0.2f) prefab = tankEnemyPrefab;
        else if (r < 0.4f) prefab = fastEnemyPrefab;
        else prefab = normalEnemyPrefab;

        if (prefab == null) return;
        GameObject e = Instantiate(prefab, sp.position + Random.insideUnitSphere * 2f, Quaternion.identity);
        e.GetComponent<EnemyController>()?.Initialize(wave);
        enemiesAlive++;
    }

    void SpawnPowerup()
    {
        GameObject[] prefabs = { powerupHealthPrefab, powerupRapidFirePrefab, powerupShieldPrefab };
        foreach (var p in prefabs)
        {
            if (p == null) continue;
            if (Random.value < 0.5f)
            {
                Vector3 pos = new Vector3(Random.Range(-15f, 15f), 0.5f, Random.Range(-15f, 15f));
                Instantiate(p, pos, Quaternion.identity);
            }
        }
    }

    public void EnemyKilled(int scoreValue, string enemyType, bool headshot)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        int pts = headshot ? scoreValue * 2 : scoreValue;
        score += pts;
        UpdateUI();
        string msg = headshot ? $"HEADSHOT! +{pts}" : $"+{pts}";
        ShowKillFeed(msg + " [" + enemyType + "]");
    }

    public void PlayerDied()
    {
        gameActive = false;
        StopAllCoroutines();
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = "SCORE: " + score + "\nWAVE: " + wave;
    }

    public void UpdateUI()
    {
        if (scoreText) scoreText.text = "SCORE\n" + score;
        if (waveText) waveText.text = "WAVE\n" + wave;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player)
        {
            if (healthText) healthText.text = Mathf.CeilToInt(player.currentHealth) + " HP";
            if (healthSlider) healthSlider.value = player.currentHealth / player.maxHealth;
            if (ammoText) ammoText.text = player.isReloading ? "RELOADING..." :
                player.currentAmmo + "/" + player.maxAmmo + (player.grenadeCount > 0 ? " | G:" + player.grenadeCount : "");
        }
    }

    public void ShowMessage(string msg, float duration)
    {
        StartCoroutine(ShowMessageCoroutine(msg, duration));
    }

    IEnumerator ShowMessageCoroutine(string msg, float duration)
    {
        if (messageText) { messageText.text = msg; messageText.gameObject.SetActive(true); }
        yield return new WaitForSeconds(duration);
        if (messageText) messageText.gameObject.SetActive(false);
    }

    public void ShowKillFeed(string msg)
    {
        if (killFeedText) { killFeedText.text = msg; killFeedTimer = 2f; }
    }

    void ShowStartPanel()
    {
        if (startPanel) startPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }
}
