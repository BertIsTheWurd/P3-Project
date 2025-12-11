using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles the Uncensor card ability - allows player to remove Censor from cards
/// </summary>
public class UncensorHandler : MonoBehaviour
{
    public static UncensorHandler Instance;
    
    [Header("References")]
    public GameManager gameManager;
    public CensorHandler censorHandler;
    
    [Header("Visual Feedback")]
    public Color censoredHighlightColor = Color.cyan;
    
    // Internal state
    private bool isSelectingCensored = false;
    private GameObject uncensorCard;
    private List<GameObject> highlightedCensored = new List<GameObject>();
    
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
    }
    
    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (censorHandler == null)
        {
            censorHandler = FindObjectOfType<CensorHandler>();
        }
    }
    
    private void Update()
    {
        if (isSelectingCensored)
        {
            // Allow ESC to cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }
            
            // Check for click on censored card
            if (Input.GetMouseButtonDown(0))
            {
                HandleCensoredSelection();
            }
        }
    }
    
    /// <summary>
    /// Called when player clicks an Uncensor card
    /// </summary>
    public void BeginUncensorSelection(GameObject uncensor)
    {
        uncensorCard = uncensor;
        isSelectingCensored = true;
        
        Debug.Log("🔓 Select a censored card to uncensor...");
        
        // Find and highlight all censored cards
        HighlightCensoredCards();
        
        if (highlightedCensored.Count == 0)
        {
            Debug.Log("🔓 No censored cards on the grid");
            CancelSelection();
        }
    }
    
    /// <summary>
    /// Find all censored cards and highlight them
    /// </summary>
    private void HighlightCensoredCards()
    {
        ClearHighlights();
        
        if (censorHandler == null) return;
        
        List<GameObject> censoredCards = censorHandler.GetCensoredCards();
        
        foreach (GameObject card in censoredCards)
        {
            if (card != null)
            {
                HighlightCard(card);
                highlightedCensored.Add(card);
            }
        }
        
        Debug.Log($"🔓 Found {highlightedCensored.Count} censored cards");
    }
    
    /// <summary>
    /// Highlight a censored card
    /// </summary>
    private void HighlightCard(GameObject card)
    {
        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = censoredHighlightColor;
        }
    }
    
    /// <summary>
    /// Remove highlights
    /// </summary>
    private void ClearHighlights()
    {
        foreach (GameObject card in highlightedCensored)
        {
            if (card != null)
            {
                SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }
        highlightedCensored.Clear();
    }
    
    /// <summary>
    /// Handle mouse click on censored card
    /// </summary>
    private void HandleCensoredSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            
            // Check if clicked on a highlighted censored card
            if (highlightedCensored.Contains(clickedObject))
            {
                StartCoroutine(ExecuteUncensorAbility(clickedObject));
            }
        }
    }
    
    /// <summary>
    /// Execute the uncensor ability - remove censor from card
    /// </summary>
    private IEnumerator ExecuteUncensorAbility(GameObject censoredCard)
    {
        isSelectingCensored = false;
        ClearHighlights();
        
        Debug.Log($"🔓 Uncensor: Removing censor from card");
        
        // Get the censor card overlay before removing it
        GameObject censorOverlay = null;
        if (censorHandler != null)
        {
            List<GameObject> censoredCards = censorHandler.GetCensoredCards();
            // Find the censor overlay for this target card
            foreach (var kvp in censorHandler.GetCensorCardDictionary())
            {
                if (kvp.Key == censoredCard)
                {
                    censorOverlay = kvp.Value;
                    break;
                }
            }
        }
        
        // Animate uncensor (optional - flash/pulse)
        yield return StartCoroutine(AnimateUncensor(censoredCard));
        
        // Remove the censor
        if (censorHandler != null)
        {
            censorHandler.RemoveCensor(censoredCard);
        }
        
        // Send censor card to discard pile if we found it
        DiscardPile discardPile = FindObjectOfType<DiscardPile>();
        if (censorOverlay != null && discardPile != null)
        {
            discardPile.AddToDiscard(censorOverlay);
        }
        
        Debug.Log("✅ Card uncensored!");
        
        // Re-validate path since uncensoring may complete a path
        if (gameManager != null)
        {
            gameManager.RevalidatePath();
        }
        
        // Remove uncensor card from hand and discard it
        CardsInHandController handController = FindObjectOfType<CardsInHandController>();
        
        if (handController != null && uncensorCard != null)
        {
            handController.RemoveCardFromHand(uncensorCard);
            
            if (discardPile != null)
            {
                discardPile.AddToDiscard(uncensorCard);
            }
            
            Debug.Log("✅ Uncensor card used and discarded");
        }
        
        uncensorCard = null;
    }
    
    /// <summary>
    /// Animate the uncensor effect (flash/pulse)
    /// </summary>
    private IEnumerator AnimateUncensor(GameObject card)
    {
        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Flash white -> cyan -> white
            Color flashColor = Color.Lerp(Color.white, censoredHighlightColor, Mathf.Sin(t * Mathf.PI * 4));
            spriteRenderer.color = flashColor;
            
            yield return null;
        }
        
        // Restore white
        spriteRenderer.color = Color.white;
    }
    
    /// <summary>
    /// Cancel selection
    /// </summary>
    private void CancelSelection()
    {
        Debug.Log("🔓 Uncensor selection cancelled");
        isSelectingCensored = false;
        ClearHighlights();
        uncensorCard = null;
    }
}