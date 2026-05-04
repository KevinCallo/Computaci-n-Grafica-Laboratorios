using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager2D : MonoBehaviour
{
    public static GameManager2D Instance;

    [Header("Game State")]
    public bool gameActive = false;
    public int collectiblesFound = 0;
    public int totalCollectibles = 0;

    [Header("UI References")]
    public TextMeshProUGUI sceneTitle;
    public TextMeshProUGUI collectiblesText;
    public TextMeshProUGUI messageText;
    public GameObject winPanel;

    [Header("Scene Roots")]
    public GameObject[] sceneRoots; // Arrastra aquí el objeto "Epic_Long_Level"

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() { StartGame(); }

    public void StartGame()
    {
        collectiblesFound = 0;
        gameActive = true;
        
        // Contar cuántas gemas hay en toda la escena
        totalCollectibles = FindObjectsByType<Collectible2D>(FindObjectsSortMode.None).Length;
        
        UpdateUI();
        ShowMessage("The Great Journey Begins!", 3f);
    }

    public void CollectibleFound()
    {
        collectiblesFound++;
        UpdateUI();
        if (collectiblesFound >= totalCollectibles && totalCollectibles > 0)
            ShowMessage("All items found! Find the exit!", 2f);
    }

    public void SceneComplete()
    {
        gameActive = false;
        if (winPanel) winPanel.SetActive(true);
        ShowMessage("YOU WIN! ★", 5f);
    }

    public void UpdateUI()
    {
        if (collectiblesText) collectiblesText.text = $"Keys: {collectiblesFound}/{totalCollectibles}";
        if (sceneTitle) sceneTitle.text = "The Long Journey";
    }

    public void ShowMessage(string msg, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(MsgCoroutine(msg, duration));
    }

    IEnumerator MsgCoroutine(string msg, float dur)
    {
        if (messageText) { messageText.text = msg; messageText.gameObject.SetActive(true); }
        yield return new WaitForSeconds(dur);
        if (messageText) messageText.gameObject.SetActive(false);
    }

    public void PlayError() { /* Sonido de error opcional */ }
}