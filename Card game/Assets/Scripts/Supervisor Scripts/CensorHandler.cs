using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles the Censor card ability - allows Supervisor to disable player cards
/// Places a censor overlay on top of existing cards
/// </summary>
public class CensorHandler : MonoBehaviour
{
    public static CensorHandler Instance;
    
    [Header("References")]
    public GameManager gameManager;
    
    [Header("Visual Settings")]
    public Color targetHighlightColor = Color.yellow;
    public GameObject censorOverlayPrefab; // Visual overlay showing card is censored
    
    // Track censored cards
    private Dictionary<GameObject, GameObject> censoredCards = new Dictionary<GameObject, GameObject>();
    
    // Selection state
    private bool isSelectingTarget = false;
    private GameObject censorCard;
    private List<GameObject> highlightedTargets = new List<GameObject>();
    
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
    }
    
    /// <summary>
    /// Called when Supervisor wants to place a Censor card
    /// </summary>
    public void BeginCensorSelection(GameObject censor)
    {
        censorCard = censor;
        isSelectingTarget = true;
        
        Debug.Log("🔒 Supervisor selecting card to censor...");
        
        // Find and highlight all player cards on grid
        HighlightPlayerCards();
        
        if (highlightedTargets.Count == 0)
        {
            Debug.Log("🔒 No player cards available to censor");
            CancelCensorSelection();
        }
    }
    
    /// <summary>
    /// Find all player cards on grid and highlight them
    /// </summary>
    private void HighlightPlayerCards()
    {
        ClearHighlights();
        
        for (int row = 0; row < gameManager.gridX; row++)
        {
            for (int col = 0; col < gameManager.gridZ; col++)
            {
                GameObject cardObj = gameManager.playedCards[row, col];
                if (cardObj != null)
                {
                    Card card = cardObj.GetComponent<Card>();
                    // Only target player cards that aren't already censored
                    if (card != null 
                        && card.cardData.cardOwner == CardOwner.Player
                        && !card.cardData.isStart 
                        && !card.cardData.isEnd
                        && !IsCensored(cardObj))
                    {
                        HighlightCard(cardObj);
                        highlightedTargets.Add(cardObj);
                    }
                }
            }
        }
        
        Debug.Log($"🔒 Found {highlightedTargets.Count} player cards to censor");
    }
    
    /// <summary>
    /// Highlight a card for selection
    /// </summary>
    private void HighlightCard(GameObject card)
    {
        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetHighlightColor;
        }
    }
    
    /// <summary>
    /// Remove highlights from all cards
    /// </summary>
    private void ClearHighlights()
    {
        foreach (GameObject card in highlightedTargets)
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
        highlightedTargets.Clear();
    }
    
    /// <summary>
    /// Choose a random player card to censor (for AI)
    /// </summary>
    public GameObject ChooseRandomTarget()
    {
        List<GameObject> validTargets = new List<GameObject>();
        
        for (int row = 0; row < gameManager.gridX; row++)
        {
            for (int col = 0; col < gameManager.gridZ; col++)
            {
                GameObject cardObj = gameManager.playedCards[row, col];
                if (cardObj != null)
                {
                    Card card = cardObj.GetComponent<Card>();
                    if (card != null 
                        && card.cardData.cardOwner == CardOwner.Player
                        && !card.cardData.isStart 
                        && !card.cardData.isEnd
                        && !IsCensored(cardObj))
                    {
                        validTargets.Add(cardObj);
                    }
                }
            }
        }
        
        if (validTargets.Count == 0)
        {
            return null;
        }
        
        return validTargets[Random.Range(0, validTargets.Count)];
    }
    
    /// <summary>
    /// Place censor card on top of a target card
    /// </summary>
    public void PlaceCensor(GameObject targetCard, GameObject censorCard = null)
    {
        if (targetCard == null) return;
        
        Debug.Log($"🔒 Censoring card at position");
        
        // Mark card as censored
        Card card = targetCard.GetComponent<Card>();
        if (card != null)
        {
            card.isCensored = true;
        }
        
        // If a censor card object was provided, track it as the overlay
        if (censorCard != null)
        {
            censoredCards[targetCard] = censorCard;
        }
        // Otherwise, create visual overlay if prefab exists
        else if (censorOverlayPrefab != null)
        {
            GameObject overlay = Instantiate(censorOverlayPrefab, targetCard.transform);
            overlay.transform.localPosition = new Vector3(0, 0.01f, 0); // Slightly above card
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;
            
            censoredCards[targetCard] = overlay;
        }
        
        // Path validation will automatically treat censored cards as broken connections
        Debug.Log("✅ Card censored - path connections disabled");
    }
    
    /// <summary>
    /// Remove censor from a card
    /// </summary>
    public void RemoveCensor(GameObject targetCard)
    {
        if (targetCard == null) return;
        
        Debug.Log($"🔓 Uncensoring card");
        
        // Unmark card
        Card card = targetCard.GetComponent<Card>();
        if (card != null)
        {
            card.isCensored = false;
        }
        
        // Remove visual overlay
        if (censoredCards.ContainsKey(targetCard))
        {
            GameObject overlay = censoredCards[targetCard];
            if (overlay != null)
            {
                Destroy(overlay);
            }
            censoredCards.Remove(targetCard);
        }
        
        Debug.Log("✅ Card uncensored - path connections restored");
    }
    
    /// <summary>
    /// Check if a card is currently censored
    /// </summary>
    public bool IsCensored(GameObject card)
    {
        if (card == null) return false;
        
        Card cardComponent = card.GetComponent<Card>();
        if (cardComponent != null)
        {
            return cardComponent.isCensored;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all currently censored cards
    /// </summary>
    public List<GameObject> GetCensoredCards()
    {
        List<GameObject> censored = new List<GameObject>();
        
        foreach (var kvp in censoredCards)
        {
            if (kvp.Key != null)
            {
                censored.Add(kvp.Key);
            }
        }
        
        return censored;
    }
    
    /// <summary>
    /// Get the dictionary mapping target cards to censor overlays (for uncensor to find the censor card)
    /// </summary>
    public Dictionary<GameObject, GameObject> GetCensorCardDictionary()
    {
        return censoredCards;
    }
    
    /// <summary>
    /// Cancel censor selection
    /// </summary>
    private void CancelCensorSelection()
    {
        Debug.Log("🔒 Censor selection cancelled");
        isSelectingTarget = false;
        ClearHighlights();
        censorCard = null;
    }
}