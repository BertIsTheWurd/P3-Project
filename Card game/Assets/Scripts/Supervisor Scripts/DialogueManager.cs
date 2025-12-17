using UnityEngine;
using UnityEngine.Events;
using TMPro;  // TextMeshPro namespace
using System.Collections;
using System.Collections.Generic;

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
    public AudioSource[] voiceLinesIntro; // Intro voice line for Supervisor 

    [System.Serializable]
    public class AudioSourceArray
    {
        public AudioSource[] audioSources;
    }
    public List<AudioSourceArray> voiceLinesCardPlacement = new List<AudioSourceArray>(); //Card placement list of voice line Arrays
    public AudioSource[] voiceLinesCensor;
    public AudioSource[] voiceLinesDeadEnd;
    public AudioSource[] voiceLinesLock;
    public AudioSource[] voiceLinesSeeMeInMyOffice;
    public AudioSource[] voiceLinesSupervisorRemoval; // Supervisor removal voice line
    public AudioSource[] voiceLinesLoss; // Supervisor loss voice line
    public AudioSource[] voiceLinesWin; // Supervisor win voice line
    private AudioSource currentlyPlayingAudio = null; // Track currently playing audio
    private bool isPlayingIntro = false;
    public UnityAction playIntroLine;
    public UnityAction<GameObject> playCardPlacementLines;
    public UnityAction playSupervisorRemovalLine;
    public UnityAction playSupervisorLossLine;
    public UnityAction playSupervisorWinLine;

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
        InitializeAudioSourceComponents();
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
    
    private void InitializeAudioSourceComponents()
    {
        voiceLinesCardPlacement.Add(new AudioSourceArray { audioSources = voiceLinesCensor });
        voiceLinesCardPlacement.Add(new AudioSourceArray { audioSources = voiceLinesDeadEnd });
        voiceLinesCardPlacement.Add(new AudioSourceArray { audioSources = voiceLinesLock });
        voiceLinesCardPlacement.Add(new AudioSourceArray { audioSources = voiceLinesSeeMeInMyOffice });

        playCardPlacementLines += PlayCardPlacementVoiceLine;
        playIntroLine += PlayIntroVoiceLine;  // Initialize intro line
        playSupervisorRemovalLine += () => PlayVoiceLine(voiceLinesSupervisorRemoval);
        playSupervisorLossLine += () => PlayVoiceLine(voiceLinesLoss);
        playSupervisorWinLine += () => PlayVoiceLine(voiceLinesWin);
    }   
    
    private void PlayVoiceLine(AudioSource[] voiceLines, bool isIntro = false)
    {
        if (voiceLines != null && voiceLines.Length > 0)
        {
            // If intro is currently playing and this is NOT an intro, skip
            if (isPlayingIntro && !isIntro)
            {
                Debug.Log("Intro is playing - skipping other voice line");
                return;
            }

            // Stop any currently playing audio (intro can interrupt anything)
            StopCurrentAudio();

            int index = Random.Range(0, voiceLines.Length);
            AudioSource selectedVoiceLine = voiceLines[index];
            if (selectedVoiceLine != null)
            {
                selectedVoiceLine.Play();
                currentlyPlayingAudio = selectedVoiceLine;
                isPlayingIntro = isIntro;

                Debug.Log($"Playing voice line: {selectedVoiceLine.name}{(isIntro ? " (INTRO - Priority)" : "")}");

                // Clear the reference when audio finishes
                StartCoroutine(ClearAudioReferenceWhenDone(selectedVoiceLine, isIntro));
            }
        }
    }

    // Add this new method to stop currently playing audio
    private void StopCurrentAudio()
    {
        if (currentlyPlayingAudio != null && currentlyPlayingAudio.isPlaying)
        {
            currentlyPlayingAudio.Stop();
            Debug.Log($"Stopped currently playing audio: {currentlyPlayingAudio.name}");
        }
        currentlyPlayingAudio = null;
        isPlayingIntro = false;
    }

    // Clear the reference when audio naturally finishes
    private IEnumerator ClearAudioReferenceWhenDone(AudioSource audio, bool wasIntro)
    {
        if (audio == null) yield break;

        // Wait until the audio is no longer playing
        while (audio != null && audio.isPlaying)
        {
            yield return null;
        }

        // Only clear if this is still the current audio
        if (currentlyPlayingAudio == audio)
        {
            currentlyPlayingAudio = null;
            if (wasIntro)
            {
                isPlayingIntro = false;
            }
        }
    }

    // Update the PlayIntroVoiceLine method:
    private void PlayIntroVoiceLine()
    {
        PlayVoiceLine(voiceLinesIntro, isIntro: true);
    }

    private void PlayCardPlacementVoiceLine(GameObject card)
    {
        if (card == null)
        {
            Debug.LogWarning("PlayCardPlacementVoiceLine: card is null");
            return;
        }

        Card cardComponent = card.GetComponent<Card>();
        if (cardComponent == null || cardComponent.cardData == null)
        {
            Debug.LogWarning("PlayCardPlacementVoiceLine: card has no Card component or cardData");
            return;
        }

        DirectionalCardData cardData = cardComponent.cardData;
        int cardTypeIndex = -1;

        switch (cardData.cardType)
        {
            case CardType.Censor:
                cardTypeIndex = 0;
                break;
            case CardType.DeadEnd:
                cardTypeIndex = 1;
                break;
            case CardType.BureaucraticBarrier:
                cardTypeIndex = 2;
                break;
            case CardType.SeeMeInMyOffice:
                cardTypeIndex = 3;
                break;
            default:
                Debug.Log($"No voice line configured for card type: {cardData.cardType}");
                return; // No voice line for this card type
        }

        // Play the voice line for this card type
        if (cardTypeIndex >= 0 && cardTypeIndex < voiceLinesCardPlacement.Count)
        {
            AudioSourceArray voiceLineArray = voiceLinesCardPlacement[cardTypeIndex];
            if (voiceLineArray != null && voiceLineArray.audioSources != null && voiceLineArray.audioSources.Length > 0)
            {
                PlayVoiceLine(voiceLineArray.audioSources);
            }
            else
            {
                Debug.LogWarning($"No audio sources assigned for card type: {cardData.cardType}");
            }
        }
    }

    // Public method to manually stop audio if needed elsewhere
    public void StopAllVoiceLines()
    {
        StopCurrentAudio();
    }
}