using UnityEngine;
using UnityEngine.Serialization; // keeps scene wiring alive across the field renames below
using UnityEngine.UI; // Legacy Text, not TextMeshPro - the project's fonts are legacy-only

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    // player lives UI
    [SerializeField] private PlayerLives playerLives; // Reference to the PlayerLives script
    [SerializeField] private GameObject life_1;
    [SerializeField] private GameObject life_2;
    [SerializeField] private GameObject life_3;

    [Header("Game Over Stats")]
    [Tooltip("Text objects on the game over canvas. Any left unassigned are simply skipped.")]
    [SerializeField] private Text enemiesDefeatedText;
    [FormerlySerializedAs("catsRescuedText")]
    [SerializeField] private Text itemsCollectedText;
    [SerializeField] private Text finalTimeText;

    [Header("HUD (live counters during play)")]
    [Tooltip("Text objects on the gameplay HUD. Any left unassigned are simply skipped.")]
    [SerializeField] private Text hudEnemiesDefeatedText;
    [FormerlySerializedAs("hudCatsRescuedText")]
    [SerializeField] private Text hudItemsCollectedText;
    [SerializeField] private Text hudTimeText;

    // The timer only needs a new string when the whole second changes, not every frame
    private int lastDisplayedSecond = -1;

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Persist across scenes
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Event-driven: PlayerLives fires OnLivesChanged on every change (damage and respawn reset)
        playerLives.OnLivesChanged.AddListener(UpdateLivesUI);
        UpdateLivesUI(playerLives.GetCurrentLives()); // initial draw

        // Same idea for the run counters. GameStats.Get() creates the tracker if it doesn't exist yet
        GameStats.Get().OnStatsChanged.AddListener(UpdateStatsHUD);
        UpdateStatsHUD(); // initial draw
    }

    private void OnDestroy()
    {
        if (playerLives != null)
            playerLives.OnLivesChanged.RemoveListener(UpdateLivesUI);
        // Instance, not Get(): during scene teardown Get() would spawn a fresh tracker just to unsubscribe from it
        if (GameStats.Instance != null)
            GameStats.Instance.OnStatsChanged.RemoveListener(UpdateStatsHUD);
    }

    private void Update()
    {
        if (hudTimeText == null || GameStats.Instance == null)
            return;
        // The timer is the one stat that polls: it changes continuously, so an event would fire every frame
        int second = Mathf.FloorToInt(GameStats.Instance.ElapsedTime);
        if (second == lastDisplayedSecond)
            return;
        lastDisplayedSecond = second;
        hudTimeText.text = GameStats.Instance.GetFormattedTime();
    }

    public void UpdateLivesUI(int currentLives)
    {
        // Update the UI based on the current number of lives
        life_1.SetActive(currentLives >= 1);
        life_2.SetActive(currentLives >= 2);
        life_3.SetActive(currentLives >= 3);
    }

    /// Live HUD counters. Event-driven off GameStats.OnStatsChanged - no polling.
    public void UpdateStatsHUD()
    {
        GameStats stats = GameStats.Get();
        if (hudEnemiesDefeatedText != null)
            hudEnemiesDefeatedText.text = stats.EnemiesDefeated.ToString();
        if (hudItemsCollectedText != null)
            hudItemsCollectedText.text = stats.ItemsCollected.ToString();
    }

    /// Fills in the run totals. Called by GameManager just before the game over screen is shown.
    public void ShowFinalStats()
    {
        GameStats stats = GameStats.Get();
        if (enemiesDefeatedText != null)
            enemiesDefeatedText.text = stats.EnemiesDefeated.ToString();
        if (itemsCollectedText != null)
            itemsCollectedText.text = stats.ItemsCollected.ToString();
        if (finalTimeText != null)
            finalTimeText.text = stats.GetFormattedTime();
    }
}
