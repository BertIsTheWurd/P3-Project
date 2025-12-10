using UnityEngine;
using TMPro;  // TextMeshPro namespace
using System.Collections;

/// <summary>
/// Manages Supervisor dialogue display
/// This is a simple placeholder - can be expanded for audio, animations, etc.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;  // Using TextMeshPro
    public float displayDuration = 3f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    
    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip supervisorVoiceClip; // For voice acting
    
    private CanvasGroup canvasGroup;
    private bool isDisplaying = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Get or add CanvasGroup for fading
        if (dialoguePanel != null)
        {
            canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            }
        }
    }
    
    private void Start()
    {
        // Hide dialogue panel at start
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show Supervisor dialogue text
    /// </summary>
    public void ShowSupervisorDialogue(string message)
    {
        if (isDisplaying)
        {
            StopAllCoroutines();
        }
        
        StartCoroutine(DisplayDialogue(message));
    }
    
    /// <summary>
    /// Display dialogue with fade in/out
    /// </summary>
    private IEnumerator DisplayDialogue(string message)
    {
        isDisplaying = true;
        
        // Set up dialogue
        if (dialogueText != null)
        {
            dialogueText.text = message;
        }
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
        
        // Fade in
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration));
        }
        
        // Play audio if available
        if (audioSource != null && supervisorVoiceClip != null)
        {
            audioSource.PlayOneShot(supervisorVoiceClip);
        }
        
        // Display for duration
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeOutDuration));
        }
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        isDisplaying = false;
    }
    
    /// <summary>
    /// Fade CanvasGroup alpha
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        
        cg.alpha = endAlpha;
    }
    
    /// <summary>
    /// Immediately hide dialogue (if needed)
    /// </summary>
    public void HideDialogue()
    {
        StopAllCoroutines();
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        isDisplaying = false;
    }
}