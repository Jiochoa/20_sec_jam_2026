using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentGameState { get; private set; }
    private GameState previousGameState;

    // Statics survive a scene reload, so a restart can tell the fresh GameManager which state to boot into
    private static bool reloadPending;
    private static GameState stateAfterReload = GameState.Main_Menu;

    private void Awake()
    {
        // Ensure only one GameManager exists in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        previousGameState = GameState.Null_State;
        if (reloadPending)
        {
            reloadPending = false;
            SetState(stateAfterReload);
        }
        else
        {
            SetState(GameState.Main_Menu); // Begin in main menu
        }
    }

    private void Update()
    {
        // Escape toggles the pause menu during gameplay
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }
        if (CurrentGameState == GameState.Playing)
        {
            OnPauseGame();
        }
        else if (CurrentGameState == GameState.Pause_Menu)
        {
            OnResumeGame();
        }
    }

    #region State Handlers
    public void SetState(GameState newState)
    {
        previousGameState = CurrentGameState;
        CurrentGameState = newState;

        switch (newState)
        {
            case GameState.Main_Menu:
                EnterMainMenu();
                break;
            case GameState.Ready_Game:
                EnterReadyGame();
                break;
            case GameState.Intro_Cutscene:
                PlayIntroCutscene();
                break;
            case GameState.Playing:
                StartGameplay();
                break;
            case GameState.Pause_Menu:
                PauseGame();
                break;
            case GameState.GameOver:
                GameOver();
                break;
            case GameState.Outro_Cutscene:
                PlayOutroCutscene();
                break;
            default:
                Debug.LogWarning("Unhandled GameState: " + newState);
                break;
        }
    }

    /// Stop time and Show Main Menu
    private void EnterMainMenu()
    {
        Time.timeScale = 0;
        MenuManager.Instance.ShowMainMenu();
        SoundManager.PlayMusic(MusicTrack.Menu);
    }
    private void EnterReadyGame()
    {
        SetState(GameState.Intro_Cutscene);
    }
    /// Handles the intro cutscene (then begins gameplay).
    private void PlayIntroCutscene()
    {
        // Cutscene playback goes here
        SetState(GameState.Playing); // Automatically move to gameplay afterward
    }
    private void StartGameplay()
    {
        Time.timeScale = 1;
        MenuManager.Instance.HideAllMenus();
        SoundManager.ResumeMusic(); // Before the track call, so an unpause picks up where it left off
        SoundManager.PlayMusic(MusicTrack.Level);
    }
    private void PauseGame()
    {
        Time.timeScale = 0;
        MenuManager.Instance.ShowPauseMenu();
        // AudioSources ignore timeScale, so the music has to be stopped by hand
        SoundManager.PauseMusic();
    }
    /// Handles the outro cutscene (then shows the end screen).
    private void PlayOutroCutscene()
    {
        // Cutscene playback goes here
        SetState(GameState.GameOver); // Automatically show the end screen afterward
    }
    /// The game was won (respawn-forever means losing lives never lands here).
    private void GameOver()
    {
        Time.timeScale = 0;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFinalStats();
        MenuManager.Instance.ShowGameOverScreen();
        SoundManager.PlayMusic(MusicTrack.Win);
    }
    #endregion

    #region Gameplay Hooks

    /// Called by GameEndTrigger when the player reaches the final level's end door.
    public void OnGameWon()
    {
        if (CurrentGameState == GameState.Playing)
        {
            GameStats.Get().StopTimer(); // Stop here, not at GameOver, so the outro cutscene isn't timed
            SoundManager.Play(SoundId.LevelComplete);
            SetState(GameState.Outro_Cutscene);
        }
    }

    #endregion

    #region UI Button Hooks

    /// Called from Pause/Game Over screen button: Main Menu. Reloads the scene so the level is pristine behind the menu.
    public void OnReturnToMainMenu() { Click(); ReloadInto(GameState.Main_Menu); }

    /// Called from UI button: Start Game.
    public void OnStartGame() { Click(); SetState(GameState.Ready_Game); }

    /// Called from UI button: Pause Game.
    public void OnPauseGame() { Click(); SetState(GameState.Pause_Menu); }

    /// Called from UI button: Resume Game.
    public void OnResumeGame() { Click(); SetState(GameState.Playing); }

    /// Called from UI button: Restart Game. Reloads the scene, then goes straight back into gameplay.
    public void OnRestartGame() { Click(); ReloadInto(GameState.Ready_Game); }

    /// The button press sound every UI hook shares.
    private void Click() => SoundManager.Play(SoundId.UIClick);

    /// Called from UI button: Quit Game.
    public void OnQuitGame()
    {
        Click();
        Debug.Log("Quit button pressed");
        // Application.Quit(); // Uncomment for builds
    }

    #endregion

    #region Helpers
    /// Full reset: reload the scene (restores despawned enemies/cats/pickups), then continue in the given state.
    private void ReloadInto(GameState nextState)
    {
        reloadPending = true;
        stateAfterReload = nextState;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    #endregion

}

#region Game State Enum
/// Enum of all possible game states. Add more as needed (e.g., Credits, Settings).
public enum GameState
{
    Intro_Cutscene,
    Main_Menu,
    Pause_Menu,
    Ready_Game,
    Playing,
    Outro_Cutscene,
    GameOver,
    Null_State, // Default value before any state is set
}
#endregion
