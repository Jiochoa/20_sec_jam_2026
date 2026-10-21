using UnityEngine;
using UnityEngine.Events;

/// Run statistics shown on the game over (win) screen: enemies defeated, items collected
/// and how long the whole run took.
/// The two counters are deliberately generic - rename them to whatever this game actually
/// counts, and remember UIManager reads them by name.
/// Deliberately holds no static state - Restart and Return To Main Menu reload the scene,
/// which destroys this object and zeroes the counters, exactly the reset those buttons want.
/// In-scene level switches don't reload, so the totals carry across all levels.
/// Self-bootstrapping like ScreenFader: Get() creates one on demand, so no scene setup is needed.
public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    public int EnemiesDefeated { get; private set; }
    public int ItemsCollected { get; private set; }
    public float ElapsedTime { get; private set; }

    /// Fired whenever a counter changes, so a HUD can redraw without polling.
    /// ElapsedTime deliberately has no event - it changes every frame, so a live timer
    /// should just read it in Update instead.
    /// Created inline because this component is usually added at runtime by Get(),
    /// where there is no serialized data to initialise it from.
    public UnityEvent OnStatsChanged = new UnityEvent();

    private bool timerStopped;

    /// Returns the tracker, creating one if the scene has none.
    public static GameStats Get()
    {
        if (Instance == null)
        {
            Instance = FindAnyObjectByType<GameStats>();
        }
        if (Instance == null)
        {
            GameObject holder = new GameObject("GameStats");
            Instance = holder.AddComponent<GameStats>();
        }
        return Instance;
    }

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Only gameplay counts. Menus set timeScale to 0 so deltaTime is already frozen while paused,
        // and the state check additionally keeps cutscene time out of the total.
        if (timerStopped || GameManager.Instance == null)
        {
            return;
        }
        if (GameManager.Instance.CurrentGameState == GameState.Playing)
        {
            ElapsedTime += Time.deltaTime;
        }
    }

    public void AddEnemyDefeated()
    {
        EnemiesDefeated++;
        OnStatsChanged.Invoke();
    }

    public void AddItemCollected()
    {
        ItemsCollected++;
        OnStatsChanged.Invoke();
    }

    /// Takes one back off the tally, for games where a collectible can be spent or lost again.
    /// Clamped at zero - the tally should never read negative on the win screen.
    public void RemoveItemCollected()
    {
        if (ItemsCollected <= 0)
        {
            return;
        }
        ItemsCollected--;
        OnStatsChanged.Invoke();
    }

    /// Called when the player reaches the final door, so the outro cutscene isn't timed.
    public void StopTimer()
    {
        timerStopped = true;
    }

    /// Run time as m:ss (e.g. "2:07").
    public string GetFormattedTime()
    {
        int totalSeconds = Mathf.FloorToInt(ElapsedTime);
        return string.Format("{0}:{1:00}", totalSeconds / 60, totalSeconds % 60);
    }
}
