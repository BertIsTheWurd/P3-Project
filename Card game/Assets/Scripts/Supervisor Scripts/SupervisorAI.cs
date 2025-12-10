using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The Supervisor AI - An omnipresent narrator/opponent that intervenes during gameplay
/// Manages Supervisor's deck, decision making, and card placement
/// </summary>
public class SupervisorAI : MonoBehaviour
{
    public static SupervisorAI Instance;
    
    [Header("References")]
    public GameManager gameManager;
    public SupervisorDeck supervisorDeck;
    public DialogueManager dialogueManager; // Optional - for future dialogue system
    
    [Header("AI Behavior Settings")]
    [Range(0f, 1f)]
    public float interventionChance = 0.5f; // 50% chance to intervene each turn
    
    [Tooltip("Minimum turns before Supervisor can start intervening")]
    public int minimumTurnsBeforeIntervention = 2;
    
    [Tooltip("Maximum cards Supervisor can have in hand")]
    public int supervisorHandSize = 3;
    
    [Header("AI Strategy")]
    public bool preferBlockingPlayerPath = true;
    public bool preferPlacingNearExit = true;
    public bool avoidDeadEndEarly = true;
    
    [Header("Timing")]
    public float thinkingDelay = 1f; // Delay before Supervisor acts
    public float cardPlacementDelay = 0.5f; // Delay after placing card
    
    // Internal state
    private List<GameObject> supervisorHand = new List<GameObject>();
    private int turnsSinceGameStart = 0;
    private bool isProcessingSupervisorTurn = false;
    
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
        
        if (supervisorDeck == null)
        {
            supervisorDeck = FindObjectOfType<SupervisorDeck>();
        }
        
        // Draw initial Supervisor hand
        DrawSupervisorCards(supervisorHandSize);
    }
    
    /// <summary>
    /// Called after player places a card - decides if Supervisor should intervene
    /// </summary>
    public void OnPlayerTurnEnd()
    {
        if (isProcessingSupervisorTurn) return;
        
        turnsSinceGameStart++;
        
        // Check if Supervisor should intervene this turn
        if (ShouldIntervene())
        {
            StartCoroutine(ProcessSupervisorTurn());
        }
        else
        {
            Debug.Log("🎭 Supervisor: Watching silently...");
        }
    }
    
    /// <summary>
    /// Decides whether the Supervisor should intervene this turn
    /// </summary>
    private bool ShouldIntervene()
    {
        // Don't intervene in first few turns
        if (turnsSinceGameStart < minimumTurnsBeforeIntervention)
        {
            return false;
        }
        
        // Don't intervene if no cards in hand
        if (supervisorHand.Count == 0)
        {
            return false;
        }
        
        // Random chance to intervene
        if (Random.value > interventionChance)
        {
            return false;
        }
        
        // TODO: Add more sophisticated logic
        // - Check if player is close to winning
        // - Check if there's a good strategic position
        // - Check card types in hand
        
        return true;
    }
    
    /// <summary>
    /// Process Supervisor's turn - think, choose card, place it
    /// </summary>
    private IEnumerator ProcessSupervisorTurn()
    {
        isProcessingSupervisorTurn = true;
        
        Debug.Log("🎭 Supervisor: *intervenes*");
        
        // Thinking delay for dramatic effect
        yield return new WaitForSeconds(thinkingDelay);
        
        // Choose a card to play
        GameObject cardToPlay = ChooseSupervisorCard();
        
        if (cardToPlay == null)
        {
            Debug.LogWarning("Supervisor wanted to intervene but couldn't choose a card");
            isProcessingSupervisorTurn = false;
            yield break;
        }
        
        // Choose where to place it
        Vector2Int? targetPosition = ChoosePlacementPosition(cardToPlay);
        
        if (!targetPosition.HasValue)
        {
            Debug.LogWarning("Supervisor couldn't find valid placement");
            isProcessingSupervisorTurn = false;
            yield break;
        }
        
        // Display dialogue (if system exists)
        string dialogue = GetInterventionDialogue(cardToPlay);
        if (dialogueManager != null)
        {
            dialogueManager.ShowSupervisorDialogue(dialogue);
        }
        else
        {
            Debug.Log($"🎭 Supervisor: \"{dialogue}\"");
        }
        
        // Place the card
        yield return new WaitForSeconds(cardPlacementDelay);
        PlaceSupervisorCard(cardToPlay, targetPosition.Value);
        
        // Draw replacement card
        DrawSupervisorCards(1);
        
        isProcessingSupervisorTurn = false;
    }
    
    /// <summary>
    /// Choose which card from Supervisor's hand to play
    /// </summary>
    private GameObject ChooseSupervisorCard()
    {
        if (supervisorHand.Count == 0) return null;
        
        // Try each card in hand to find one with valid placements
        List<GameObject> playableCards = new List<GameObject>();
        
        foreach (GameObject card in supervisorHand)
        {
            List<Vector2Int> validPositions = GetValidPlacementPositions(card);
            if (validPositions.Count > 0)
            {
                playableCards.Add(card);
            }
        }
        
        if (playableCards.Count == 0)
        {
            Debug.Log("🎭 Supervisor: Has cards but none can be placed (no adjacent player cards yet)");
            return null;
        }
        
        // TODO: Implement smarter card selection
        // - Prioritize blocking cards when player is progressing
        // - Use specific cards for specific situations
        
        // For now, pick random playable card
        int randomIndex = Random.Range(0, playableCards.Count);
        return playableCards[randomIndex];
    }
    
    /// <summary>
    /// Choose where to place the Supervisor's card
    /// </summary>
    private Vector2Int? ChoosePlacementPosition(GameObject card)
    {
        List<Vector2Int> validPositions = GetValidPlacementPositions(card);
        
        if (validPositions.Count == 0)
        {
            Debug.Log($"🎭 Supervisor: No valid positions for {card.GetComponent<Card>().cardData.cardName} (must connect to player cards)");
            return null;
        }
        
        Debug.Log($"🎭 Supervisor: Found {validPositions.Count} valid positions for {card.GetComponent<Card>().cardData.cardName}");
        
        // TODO: Implement smarter positioning logic
        // - Block player's most likely path
        // - Place near critical junctions
        // - Create obstacles strategically
        
        // For now, pick random valid position
        int randomIndex = Random.Range(0, validPositions.Count);
        return validPositions[randomIndex];
    }
    
    /// <summary>
    /// Get all valid positions where this card can be placed
    /// Supervisor can only place cards adjacent to player's existing cards (not start/end)
    /// Exception: Censor cards can target any player card on the grid
    /// </summary>
    private List<Vector2Int> GetValidPlacementPositions(GameObject card)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        Card cardComponent = card.GetComponent<Card>();
        if (cardComponent == null) return validPositions;
        
        // Special case: Censor cards target existing player cards
        if (cardComponent.cardData.cardType == CardType.Censor)
        {
            CensorHandler censorHandler = FindObjectOfType<CensorHandler>();
            if (censorHandler != null)
            {
                // Find all non-censored player cards
                for (int row = 0; row < gameManager.gridX; row++)
                {
                    for (int col = 0; col < gameManager.gridZ; col++)
                    {
                        GameObject targetCard = gameManager.playedCards[row, col];
                        if (targetCard != null)
                        {
                            Card target = targetCard.GetComponent<Card>();
                            if (target != null 
                                && target.cardData.cardOwner == CardOwner.Player
                                && !target.cardData.isStart
                                && !target.cardData.isEnd
                                && !censorHandler.IsCensored(targetCard))
                            {
                                // Vector2Int is (col, row) or (x, y)
                                validPositions.Add(new Vector2Int(col, row));
                            }
                        }
                    }
                }
            }
            return validPositions;
        }
        
        // Normal card placement logic (adjacent to player cards)
        for (int row = 0; row < gameManager.gridX; row++)
        {
            for (int col = 0; col < gameManager.gridZ; col++)
            {
                // Check if position is valid using GameManager rules
                if (!gameManager.CanPlaceCard(col, row, card))
                {
                    continue;
                }
                
                // Additional Supervisor rule: Must be adjacent to a player-placed card
                // (not start or end cards)
                if (IsAdjacentToPlayerCard(row, col))
                {
                    validPositions.Add(new Vector2Int(col, row));
                }
            }
        }
        
        return validPositions;
    }
    
    /// <summary>
    /// Check if a position is adjacent to a player-placed card
    /// OR adjacent to start/end cards that already have player cards connected to them
    /// </summary>
    private bool IsAdjacentToPlayerCard(int row, int col)
    {
        // Check all four adjacent positions
        // Up
        if (row > 0 && gameManager.playedCards[row - 1, col] != null)
        {
            Card adjacentCard = gameManager.playedCards[row - 1, col].GetComponent<Card>();
            if (adjacentCard != null)
            {
                // Allow if it's a regular PLAYER card (not Supervisor card)
                if (!adjacentCard.cardData.isStart && !adjacentCard.cardData.isEnd 
                    && adjacentCard.cardData.cardOwner == CardOwner.Player)
                {
                    return true;
                }
                
                // Allow if it's start/end but has player cards connected to it
                if (adjacentCard.cardData.isStart || adjacentCard.cardData.isEnd)
                {
                    if (HasPlayerCardConnected(row - 1, col))
                    {
                        return true;
                    }
                }
            }
        }
        
        // Down
        if (row < gameManager.gridX - 1 && gameManager.playedCards[row + 1, col] != null)
        {
            Card adjacentCard = gameManager.playedCards[row + 1, col].GetComponent<Card>();
            if (adjacentCard != null)
            {
                if (!adjacentCard.cardData.isStart && !adjacentCard.cardData.isEnd 
                    && adjacentCard.cardData.cardOwner == CardOwner.Player)
                {
                    return true;
                }
                
                if (adjacentCard.cardData.isStart || adjacentCard.cardData.isEnd)
                {
                    if (HasPlayerCardConnected(row + 1, col))
                    {
                        return true;
                    }
                }
            }
        }
        
        // Left
        if (col > 0 && gameManager.playedCards[row, col - 1] != null)
        {
            Card adjacentCard = gameManager.playedCards[row, col - 1].GetComponent<Card>();
            if (adjacentCard != null)
            {
                if (!adjacentCard.cardData.isStart && !adjacentCard.cardData.isEnd 
                    && adjacentCard.cardData.cardOwner == CardOwner.Player)
                {
                    return true;
                }
                
                if (adjacentCard.cardData.isStart || adjacentCard.cardData.isEnd)
                {
                    if (HasPlayerCardConnected(row, col - 1))
                    {
                        return true;
                    }
                }
            }
        }
        
        // Right
        if (col < gameManager.gridZ - 1 && gameManager.playedCards[row, col + 1] != null)
        {
            Card adjacentCard = gameManager.playedCards[row, col + 1].GetComponent<Card>();
            if (adjacentCard != null)
            {
                if (!adjacentCard.cardData.isStart && !adjacentCard.cardData.isEnd 
                    && adjacentCard.cardData.cardOwner == CardOwner.Player)
                {
                    return true;
                }
                
                if (adjacentCard.cardData.isStart || adjacentCard.cardData.isEnd)
                {
                    if (HasPlayerCardConnected(row, col + 1))
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a start/end card has any PLAYER cards (not Supervisor cards) connected to it
    /// </summary>
    private bool HasPlayerCardConnected(int row, int col)
    {
        GameObject cardAtPosition = gameManager.playedCards[row, col];
        if (cardAtPosition == null) return false;
        
        Card card = cardAtPosition.GetComponent<Card>();
        if (card == null) return false;
        
        // Check all four adjacent positions for PLAYER cards
        // Up
        if (row > 0 && gameManager.playedCards[row - 1, col] != null)
        {
            Card adjacentCard = gameManager.playedCards[row - 1, col].GetComponent<Card>();
            if (adjacentCard != null 
                && !adjacentCard.cardData.isStart 
                && !adjacentCard.cardData.isEnd
                && adjacentCard.cardData.cardOwner == CardOwner.Player)  // Must be Player card
            {
                // Check if they're actually connected
                if (card.ConnectsUp && adjacentCard.ConnectsDown)
                {
                    return true;
                }
            }
        }
        
        // Down
        if (row < gameManager.gridX - 1 && gameManager.playedCards[row + 1, col] != null)
        {
            Card adjacentCard = gameManager.playedCards[row + 1, col].GetComponent<Card>();
            if (adjacentCard != null 
                && !adjacentCard.cardData.isStart 
                && !adjacentCard.cardData.isEnd
                && adjacentCard.cardData.cardOwner == CardOwner.Player)  // Must be Player card
            {
                if (card.ConnectsDown && adjacentCard.ConnectsUp)
                {
                    return true;
                }
            }
        }
        
        // Left
        if (col > 0 && gameManager.playedCards[row, col - 1] != null)
        {
            Card adjacentCard = gameManager.playedCards[row, col - 1].GetComponent<Card>();
            if (adjacentCard != null 
                && !adjacentCard.cardData.isStart 
                && !adjacentCard.cardData.isEnd
                && adjacentCard.cardData.cardOwner == CardOwner.Player)  // Must be Player card
            {
                if (card.ConnectsLeft && adjacentCard.ConnectsRight)
                {
                    return true;
                }
            }
        }
        
        // Right
        if (col < gameManager.gridZ - 1 && gameManager.playedCards[row, col + 1] != null)
        {
            Card adjacentCard = gameManager.playedCards[row, col + 1].GetComponent<Card>();
            if (adjacentCard != null 
                && !adjacentCard.cardData.isStart 
                && !adjacentCard.cardData.isEnd
                && adjacentCard.cardData.cardOwner == CardOwner.Player)  // Must be Player card
            {
                if (card.ConnectsRight && adjacentCard.ConnectsLeft)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Actually place the Supervisor's card on the grid
    /// </summary>
    private void PlaceSupervisorCard(GameObject card, Vector2Int position)
    {
        // Remove from Supervisor's hand
        supervisorHand.Remove(card);
        
        Card cardComponent = card.GetComponent<Card>();
        
        // Check if this is a Censor card
        if (cardComponent != null && cardComponent.cardData.cardType == CardType.Censor)
        {
            // Censor cards are placed ON TOP of existing cards, not in a new grid slot
            Debug.Log($"🎭 Supervisor attempting to censor at position ({position.x}, {position.y}) = array index [{ position.y}, {position.x}]");
            GameObject targetCard = gameManager.playedCards[position.y, position.x];
            
            if (targetCard != null)
            {
                Card targetCardComponent = targetCard.GetComponent<Card>();
                Debug.Log($"🎭 Supervisor censoring card at ({position.x}, {position.y}) - Target: {(targetCardComponent != null ? targetCardComponent.cardData.cardName : "UNKNOWN")}");
                
                // Position the Censor card on top of the target card
                card.transform.position = targetCard.transform.position + new Vector3(0, 0.02f, 0);
                card.transform.rotation = Quaternion.Euler(90, 0, 0); // Lie flat
                card.transform.localScale = Vector3.one * 0.25f;
                
                // Mark the target card as censored
                if (targetCardComponent != null)
                {
                    targetCardComponent.isCensored = true;
                }
                
                // Track the censor overlay in CensorHandler
                CensorHandler censorHandler = FindObjectOfType<CensorHandler>();
                if (censorHandler != null)
                {
                    // Store the censor card object so we can remove it later
                    censorHandler.PlaceCensor(targetCard, card);
                }
            }
            else
            {
                Debug.LogError($"❌ CENSOR TARGET IS NULL at position ({position.x}, {position.y})!");
            }
        }
        else
        {
            // Normal card placement
            gameManager.PlayCard(card, position.x, position.y);
            Debug.Log($"🎭 Supervisor placed {cardComponent.cardData.cardName} at ({position.x}, {position.y})");
        }
        
        // Check if this placement blocked all paths to correct exit
        gameManager.CheckForGameOver();
    }
    
    /// <summary>
    /// Draw cards for the Supervisor
    /// </summary>
    private void DrawSupervisorCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (supervisorHand.Count >= supervisorHandSize)
            {
                break; // Hand is full
            }
            
            GameObject card = supervisorDeck.DrawCard();
            if (card != null)
            {
                supervisorHand.Add(card);
                card.SetActive(false); // Keep Supervisor cards hidden
                Debug.Log($"🎭 Supervisor drew: {card.GetComponent<Card>().cardData.cardName}");
            }
        }
    }
    
    /// <summary>
    /// Get dialogue line for this intervention
    /// </summary>
    private string GetInterventionDialogue(GameObject card)
    {
        Card cardComponent = card.GetComponent<Card>();
        if (cardComponent == null) return "Interesting...";
        
        // TODO: Create proper dialogue system with multiple lines per card type
        switch (cardComponent.cardData.cardType)
        {
            case CardType.DeadEnd:
                return "I'm afraid this path is closed.";
            
            case CardType.BureaucraticBarrier:
                return "You'll need proper authorization for this.";
            
            case CardType.Censor:
                return "This information is classified.";
            
            case CardType.SeeMeInMyOffice:
                return "We need to have a conversation. Now.";
            
            default:
                return "Let me assist you with this.";
        }
    }
    
    /// <summary>
    /// Get current Supervisor hand size (for debugging/UI)
    /// </summary>
    public int GetSupervisorHandCount()
    {
        return supervisorHand.Count;
    }
    
    /// <summary>
    /// Force Supervisor to intervene (for testing)
    /// </summary>
    public void ForceIntervention()
    {
        if (!isProcessingSupervisorTurn)
        {
            StartCoroutine(ProcessSupervisorTurn());
        }
    }
}