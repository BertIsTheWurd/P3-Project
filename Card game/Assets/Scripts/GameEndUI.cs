using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the end game UI for both Victory and Game Over states.
/// Attach this to a Canvas GameObject with the end game panel as a child.
/// </summary>
public class GameEndUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The panel containing all end game UI elements - will be shown/hidden")]
    public GameObject endGamePanel;
    
    [Tooltip("The main message text")]
    public TextMeshProUGUI messageText;
    
    [Tooltip("Button to restart the game")]
    public Button playAgainButton;
    
    [Tooltip("Button to quit the game")]
    public Button quitButton;
    
    [Header("Settings")]
    [Tooltip("The scene name to load when Play Again is clicked (leave empty to reload current scene)")]
    public string gameSceneName = "";
    
    [Header("Victory Settings")]
    [Tooltip("Victory message to display")]
    [TextArea(2, 4)]
    public string victoryMessage = "Good job, Commissaire!\nYou found the evidence we needed!";
    
    [Tooltip("Color for victory message (optional)")]
    public Color victoryColor = Color.white;
    
    [Header("Game Over Settings")]
    [Tooltip("Game Over message to display")]
    [TextArea(2, 4)]
    public string gameOverMessage = "The Ministry of Truth\nexpected more from you...";
    
    [Tooltip("Color for game over message (optional)")]
    public Color gameOverColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Reddish
    
    [Header("Pause Menu Settings")]
    [Tooltip("Pause menu message to display")]
    [TextArea(2, 4)]
    public string pauseMessage = "Game Paused";
    
    [Tooltip("Color for pause message")]
    public Color pauseColor = Color.white;
    
    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float panelScaleStart = 0.8f;
    
    private CanvasGroup canvasGroup;
    private bool isAnimating = false;

    private void Awake()
    {
        // Ensure the panel starts hidden
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }
        
        // Setup button listeners
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        // Get or add CanvasGroup for fade animation
        if (endGamePanel != null)
        {
            canvasGroup = endGamePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = endGamePanel.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// Show the victory screen with default message
    /// </summary>
    public void ShowVictory()
    {
        ShowEndScreen(victoryMessage, victoryColor);
        Debug.Log("Victory UI displayed");
    }
    
    /// <summary>
    /// Show the victory screen with a custom message
    /// </summary>
    public void ShowVictory(string customMessage)
    {
        ShowEndScreen(customMessage, victoryColor);
        Debug.Log("Victory UI displayed");
    }
    
    /// <summary>
    /// Show the game over screen with default message
    /// </summary>
    public void ShowGameOver()
    {
        ShowEndScreen(gameOverMessage, gameOverColor);
        Debug.Log("Game Over UI displayed");
    }
    
    /// <summary>
    /// Show the game over screen with a custom message
    /// </summary>
    public void ShowGameOver(string customMessage)
    {
        ShowEndScreen(customMessage, gameOverColor);
        Debug.Log("Game Over UI displayed");
    }
    
    /// <summary>
    /// Show the pause menu (can be toggled with Escape)
    /// </summary>
    public void ShowPauseMenu()
    {
        // If already showing, hide it instead (toggle behavior)
        if (endGamePanel != null && endGamePanel.activeSelf)
        {
            Hide();
            return;
        }
        
        ShowEndScreen(pauseMessage, pauseColor);
        Debug.Log("Pause Menu displayed");
    }
    
    /// <summary>
    /// Internal method to show the end screen with specified message and color
    /// </summary>
    private void ShowEndScreen(string message, Color textColor)
    {
        if (endGamePanel == null)
        {
            Debug.LogError("GameEndUI: End game panel is not assigned!");
            return;
        }
        
        // Set the message and color
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = textColor;
        }
        
        // Show the panel
        endGamePanel.SetActive(true);
        
        // Start fade-in animation
        StartCoroutine(AnimatePanel());
        
        // Pause the game
        Time.timeScale = 0f;
    }
    
    private System.Collections.IEnumerator AnimatePanel()
    {
        isAnimating = true;
        
        // Setup initial state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        RectTransform panelRect = endGamePanel.GetComponent<RectTransform>();
        Vector3 targetScale = panelRect != null ? panelRect.localScale : Vector3.one;
        
        if (panelRect != null)
        {
            panelRect.localScale = targetScale * panelScaleStart;
        }
        
        // Animate
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            // Use unscaledDeltaTime since Time.timeScale is 0
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            
            // Ease out cubic for smooth deceleration
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = easedT;
            }
            
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(targetScale * panelScaleStart, targetScale, easedT);
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        if (panelRect != null)
        {
            panelRect.localScale = targetScale;
        }
        
        isAnimating = false;
    }

    /// <summary>
    /// Hide the end game panel
    /// </summary>
    public void Hide()
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }
        
        Time.timeScale = 1f;
    }

    private void OnPlayAgainClicked()
    {
        Debug.Log("Play Again clicked");
        
        // Resume time before loading
        Time.timeScale = 1f;
        
        // Reload the current scene or load specified scene
        if (string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
        
        // Resume time (good practice)
        Time.timeScale = 1f;
        
#if UNITY_EDITOR
        // Stop playing in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the application in a build
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }
        
        // Ensure time scale is restored if this object is destroyed
        Time.timeScale = 1f;
    }
}