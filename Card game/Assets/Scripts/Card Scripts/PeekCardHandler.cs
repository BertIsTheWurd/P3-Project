using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the Peek card selection process - allows player to choose which exit to peek at
/// </summary>
public class PeekCardHandler : MonoBehaviour
{
    public static PeekCardHandler Instance;
    
    private GameManager gameManager;
    private CardAbilityHandler abilityHandler;
    private CardsInHandController handController;
    
    private bool isSelectingPeekTarget = false;
    private GameObject peekCardInHand = null;
    
    [Header("Visual Feedback")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.8f); // Yellow highlight
    public Color normalColor = Color.white;
    
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
        gameManager = FindObjectOfType<GameManager>();
        abilityHandler = FindObjectOfType<CardAbilityHandler>();
        handController = FindObjectOfType<CardsInHandController>();
        
        if (gameManager == null)
        {
            Debug.LogError("PeekCardHandler: GameManager not found!");
        }
        if (abilityHandler == null)
        {
            Debug.LogError("PeekCardHandler: CardAbilityHandler not found!");
        }
    }
    
    private void Update()
    {
        if (isSelectingPeekTarget)
        {
            HandlePeekTargetSelection();
        }
    }
    
    /// <summary>
    /// Called when player selects a Peek card from their hand
    /// </summary>
    public void BeginPeekSelection(GameObject peekCard)
    {
        if (peekCard == null)
        {
            Debug.LogError("PeekCardHandler: Peek card is null!");
            return;
        }
        
        Card card = peekCard.GetComponent<Card>();
        if (card == null || card.cardData.cardType != CardType.Peek)
        {
            Debug.LogError("PeekCardHandler: Card is not a Peek card!");
            return;
        }
        
        peekCardInHand = peekCard;
        isSelectingPeekTarget = true;
        
        Debug.Log("🔍 Select an exit card to peek at...");
        
        // Highlight all end cards
        HighlightEndCards(true);
    }
    
    /// <summary>
    /// Handle mouse clicks to select which exit card to peek at
    /// </summary>
    private void HandlePeekTargetSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                Card clickedCard = clickedObject.GetComponent<Card>();
                
                // Check if we clicked an end card
                if (clickedCard != null && clickedCard.cardData.isEnd)
                {
                    // Get the row of the end card
                    Vector2Int? cardPos = gameManager.GetCardPosition(clickedObject);
                    
                    if (cardPos.HasValue)
                    {
                        int targetRow = cardPos.Value.y;
                        StartCoroutine(ExecutePeekAbility(targetRow));
                    }
                }
                else
                {
                    // Clicked somewhere else - cancel peek
                    CancelPeek();
                }
            }
            else
            {
                // Clicked empty space - cancel peek
                CancelPeek();
            }
        }
        
        // Allow ESC key to cancel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPeek();
        }
    }
    
    /// <summary>
    /// Execute the peek ability on the selected exit
    /// </summary>
    private IEnumerator ExecutePeekAbility(int targetRow)
    {
        Debug.Log($"🔍 Peeking at exit row {targetRow}");
        
        // Remove highlight from all end cards
        HighlightEndCards(false);
        
        // Get the peek card data
        Card peekCard = peekCardInHand.GetComponent<Card>();
        
        // Execute the peek ability
        yield return abilityHandler.ExecuteCardAbility(
            peekCard.cardData,
            sourceCard: peekCardInHand,
            targetRow: targetRow
        );
        
        // Remove peek card from hand
        handController.RemoveCardFromHand(peekCardInHand);
        
        // Send peek card to discard pile
        gameManager.DiscardCard(peekCardInHand);
        
        // Clean up
        isSelectingPeekTarget = false;
        peekCardInHand = null;
        
        Debug.Log("✅ Peek card used and discarded");
    }
    
    /// <summary>
    /// Cancel the peek selection
    /// </summary>
    private void CancelPeek()
    {
        Debug.Log("Peek cancelled");
        
        HighlightEndCards(false);
        isSelectingPeekTarget = false;
        peekCardInHand = null;
    }
    
    /// <summary>
    /// Highlight or un-highlight all end cards
    /// </summary>
    private void HighlightEndCards(bool highlight)
    {
        int endColumn = gameManager.gridZ - 1;
        int[] endRows = { 0, 2, 4 };
        
        foreach (int row in endRows)
        {
            GameObject endCard = gameManager.playedCards[row, endColumn];
            if (endCard != null)
            {
                SpriteRenderer sr = endCard.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = highlight ? highlightColor : normalColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Check if we're currently selecting a peek target
    /// </summary>
    public bool IsSelectingPeekTarget()
    {
        return isSelectingPeekTarget;
    }
}