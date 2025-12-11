using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the Victory screen UI when the player finds the correct exit.
/// Attach this to a Canvas GameObject with the victory panel as a child.
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The panel containing all victory UI elements - will be shown/hidden")]
    public GameObject victoryPanel;
    
    [Tooltip("The main victory message text")]
    public TextMeshProUGUI victoryMessageText;
    
    [Tooltip("Button to restart the game")]
    public Button playAgainButton;
    
    [Tooltip("Button to quit the game")]
    public Button quitButton;
    
    [Header("Settings")]
    [Tooltip("The scene name to load when Play Again is clicked (leave empty to reload current scene)")]
    public string gameSceneName = "";
    
    [Tooltip("Victory message to display")]
    [TextArea(2, 4)]
    public string victoryMessage = "Good job, Commissaire!\nYou found the evidence we needed!";
    
    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float panelScaleStart = 0.8f;
    
    private CanvasGroup canvasGroup;
    private bool isAnimating = false;

    private void Awake()
    {
        // Ensure the victory panel starts hidden
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
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
        if (victoryPanel != null)
        {
            canvasGroup = victoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = victoryPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// Call this method to show the victory screen
    /// </summary>
    public void ShowVictory()
    {
        ShowVictory(victoryMessage);
    }
    
    /// <summary>
    /// Call this method to show the victory screen with a custom message
    /// </summary>
    public void ShowVictory(string customMessage)
    {
        if (victoryPanel == null)
        {
            Debug.LogError("VictoryUI: Victory panel is not assigned!");
            return;
        }
        
        // Set the message
        if (victoryMessageText != null)
        {
            victoryMessageText.text = customMessage;
        }
        
        // Show the panel
        victoryPanel.SetActive(true);
        
        // Start fade-in animation
        StartCoroutine(AnimateVictoryPanel());
        
        // Pause the game
        Time.timeScale = 0f;
        
        Debug.Log("Victory UI displayed");
    }
    
    private System.Collections.IEnumerator AnimateVictoryPanel()
    {
        isAnimating = true;
        
        // Setup initial state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        RectTransform panelRect = victoryPanel.GetComponent<RectTransform>();
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
    /// Hide the victory panel (if needed)
    /// </summary>
    public void HideVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
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