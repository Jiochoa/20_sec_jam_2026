using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Menu Canvases")]
    [Tooltip("UI Canvas for the main menu.")]
    public GameObject mainMenuCanvas;

    [Tooltip("UI Canvas for the pause menu.")]
    public GameObject pauseMenuCanvas;

    [Tooltip("UI Canvas for the game over (win) screen.")]
    public GameObject gameOverCanvas;

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

    /// Displays the main menu.
    public void ShowMainMenu()
    {
        HideAllMenus();
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(true);
        else
            Debug.LogWarning("Main Menu Canvas is not assigned.");
    }

    /// Displays the pause menu.
    public void ShowPauseMenu()
    {
        HideAllMenus();
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);
        else
            Debug.LogWarning("Pause Menu Canvas is not assigned.");
    }



    /// Displays the game over (win) screen.
    public void ShowGameOverScreen()
    {
        HideAllMenus();
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);
        else
            Debug.LogWarning("Game Over Canvas is not assigned.");
    }

    /// Hides all menus.
    public void HideAllMenus()
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
    }

}
