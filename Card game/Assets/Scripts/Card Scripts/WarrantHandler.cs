using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles the Warrant card ability - allows player to remove Bureaucratic Barriers
/// Similar to PeekCardHandler but targets barriers instead of exits
/// </summary>
public class WarrantHandler : MonoBehaviour
{
    public static WarrantHandler Instance;
    
    [Header("References")]
    public GameManager gameManager;
    
    [Header("Visual Feedback")]
    public Color barrierHighlightColor = Color.red;
    
    // Internal state
    private bool isSelectingBarrier = false;
    private GameObject warrantCard;
    private List<GameObject> highlightedBarriers = new List<GameObject>();
    
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
    
    private void Update()
    {
        if (isSelectingBarrier)
        {
            // Allow ESC to cancel selection
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBarrierSelection();
            }
            
            // Check for click on highlighted barrier
            if (Input.GetMouseButtonDown(0))
            {
                HandleBarrierSelection();
            }
        }
    }
    
    /// <summary>
    /// Called when player clicks a Warrant card
    /// </summary>
    public void BeginBarrierSelection(GameObject warrant)
    {
        warrantCard = warrant;
        isSelectingBarrier = true;
        
        Debug.Log("📜 Select a Bureaucratic Barrier to remove with Warrant...");
        
        // Find and highlight all Bureaucratic Barriers on the grid
        HighlightAllBarriers();
        
        if (highlightedBarriers.Count == 0)
        {
            Debug.Log("📜 No Bureaucratic Barriers on the grid to remove");
            CancelBarrierSelection();
        }
    }
    
    /// <summary>
    /// Find all Bureaucratic Barriers on grid and highlight them
    /// </summary>
    private void HighlightAllBarriers()
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
                    if (card != null && card.cardData.cardType == CardType.BureaucraticBarrier)
                    {
                        // Highlight this barrier
                        HighlightBarrier(cardObj);
                        highlightedBarriers.Add(cardObj);
                    }
                }
            }
        }
        
        Debug.Log($"📜 Found {highlightedBarriers.Count} Bureaucratic Barriers");
    }
    
    /// <summary>
    /// Highlight a barrier card for selection
    /// </summary>
    private void HighlightBarrier(GameObject barrier)
    {
        SpriteRenderer spriteRenderer = barrier.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = barrierHighlightColor;
        }
    }
    
    /// <summary>
    /// Remove highlights from all barriers
    /// </summary>
    private void ClearHighlights()
    {
        foreach (GameObject barrier in highlightedBarriers)
        {
            if (barrier != null)
            {
                SpriteRenderer spriteRenderer = barrier.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }
        highlightedBarriers.Clear();
    }
    
    /// <summary>
    /// Handle mouse click on a barrier
    /// </summary>
    private void HandleBarrierSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            
            // Check if clicked on a highlighted barrier
            if (highlightedBarriers.Contains(clickedObject))
            {
                StartCoroutine(ExecuteWarrantAbility(clickedObject));
            }
        }
    }
    
    /// <summary>
    /// Execute the warrant ability - remove the barrier
    /// </summary>
    private IEnumerator ExecuteWarrantAbility(GameObject barrier)
    {
        isSelectingBarrier = false;
        ClearHighlights();
        
        Debug.Log($"📜 Warrant: Removing Bureaucratic Barrier at position");
            
        // Animate barrier removal (optional - pulse/fade)
        yield return StartCoroutine(AnimateBarrierRemoval(barrier));
        
        // Remove the barrier from the grid
        gameManager.RemoveCard(barrier);
        
        // Move barrier to discard pile
        DiscardPile discardPile = FindObjectOfType<DiscardPile>();
        if (discardPile != null)
        {
            discardPile.AddToDiscard(barrier);
        }
        else
        {
            Destroy(barrier);
        }
        
        Debug.Log("✅ Bureaucratic Barrier removed!");
        
        // Remove warrant card from hand and discard it
        CardsInHandController handController = FindObjectOfType<CardsInHandController>();
        if (handController != null && warrantCard != null)
        {
            handController.RemoveCardFromHand(warrantCard);
            
            if (discardPile != null)
            {
                discardPile.AddToDiscard(warrantCard);
            }
            
            Debug.Log("✅ Warrant card used and discarded");
        }
        
        warrantCard = null;
        
        // Draw cards until hand is full (same as playing a directional card)
        if (gameManager != null)
        {
            gameManager.DrawUntilFullHand();
            
            // Notify Supervisor that player's turn ended
            SupervisorAI supervisor = FindObjectOfType<SupervisorAI>();
            if (supervisor != null)
            {
                supervisor.OnPlayerTurnEnd();
            }
        }
    }
    
    /// <summary>
    /// Animate the barrier being removed (pulse/fade effect)
    /// </summary>
    private IEnumerator AnimateBarrierRemoval(GameObject barrier)
    {
        SpriteRenderer spriteRenderer = barrier.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = barrier.transform.localScale;
        Color originalColor = spriteRenderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Pulse and fade
            float scale = Mathf.Lerp(1f, 1.2f, Mathf.Sin(t * Mathf.PI));
            barrier.transform.localScale = originalScale * scale;
            
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = newColor;
            
            yield return null;
        }
        
        // Restore original appearance (will be removed anyway)
        barrier.transform.localScale = originalScale;
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Cancel barrier selection
    /// </summary>
    private void CancelBarrierSelection()
    {
        Debug.Log("📜 Warrant selection cancelled");
        isSelectingBarrier = false;
        ClearHighlights();
        warrantCard = null;
    }
}