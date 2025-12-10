using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the Supervisor's deck - separate from player deck
/// Only contains cards that the Supervisor can play
/// </summary>
public class SupervisorDeck : MonoBehaviour
{
    [Header("References")]
    public GameObject cardPrefab;
    public DeckDefinition supervisorDeckDefinition; // Supervisor's cards only
    public Transform supervisorCardHolder; // Hidden container for Supervisor cards
    
    [Header("Discard/Reshuffle")]
    public bool autoReshuffle = true;
    
    private Stack<GameObject> drawStack = new Stack<GameObject>();
    private List<GameObject> discardedCards = new List<GameObject>();
    private List<GameObject> allSupervisorCards = new List<GameObject>();
    
    private void Start()
    {
        InitializeSupervisorDeck();
    }
    
    /// <summary>
    /// Initialize the Supervisor's deck
    /// </summary>
    private void InitializeSupervisorDeck()
    {
        if (supervisorDeckDefinition == null)
        {
            Debug.LogError("SupervisorDeck: No deck definition assigned!");
            return;
        }
        
        if (cardPrefab == null)
        {
            Debug.LogError("SupervisorDeck: No card prefab assigned!");
            return;
        }
        
        // Create all Supervisor cards based on deck definition
        for (int i = 0; i < supervisorDeckDefinition.cards.Count; i++)
        {
            DirectionalCardData cardData = supervisorDeckDefinition.cards[i];
            
            // Only include Supervisor cards
            if (cardData.cardOwner != CardOwner.Supervisor)
            {
                Debug.LogWarning($"Card {cardData.cardName} is not a Supervisor card, skipping...");
                continue;
            }
            
            int quantity = i < supervisorDeckDefinition.quantities.Count 
                ? supervisorDeckDefinition.quantities[i] 
                : 1;
            
            for (int j = 0; j < quantity; j++)
            {
                GameObject card = Instantiate(cardPrefab, supervisorCardHolder);
                card.GetComponent<Card>().SetCardData(cardData);
                card.tag = "SupervisorCard";
                card.SetActive(false); // Keep hidden
                allSupervisorCards.Add(card);
            }
        }
        
        Debug.Log($"🎭 Supervisor deck created with {allSupervisorCards.Count} cards");
        
        // Shuffle and prepare draw stack
        ShuffleDeck();
    }
    
    /// <summary>
    /// Shuffle all cards into the draw stack
    /// </summary>
    private void ShuffleDeck()
    {
        List<GameObject> cardsToShuffle = new List<GameObject>(allSupervisorCards);
        
        // Add discarded cards back
        cardsToShuffle.AddRange(discardedCards);
        discardedCards.Clear();
        
        // Fisher-Yates shuffle
        drawStack.Clear();
        while (cardsToShuffle.Count > 0)
        {
            int randomIndex = Random.Range(0, cardsToShuffle.Count);
            GameObject card = cardsToShuffle[randomIndex];
            drawStack.Push(card);
            cardsToShuffle.RemoveAt(randomIndex);
        }
        
        Debug.Log($"🎭 Supervisor deck shuffled: {drawStack.Count} cards");
    }
    
    /// <summary>
    /// Draw a card from the Supervisor's deck
    /// </summary>
    public GameObject DrawCard()
    {
        // Reshuffle if deck is empty
        if (drawStack.Count == 0)
        {
            if (autoReshuffle && discardedCards.Count > 0)
            {
                Debug.Log("🎭 Supervisor deck empty, reshuffling...");
                ShuffleDeck();
            }
            else
            {
                Debug.LogWarning("🎭 Supervisor deck is empty!");
                return null;
            }
        }
        
        if (drawStack.Count == 0)
        {
            return null;
        }
        
        GameObject card = drawStack.Pop();
        card.SetActive(true);
        
        Debug.Log($"🎭 Supervisor deck: Drew {card.GetComponent<Card>().cardData.cardName}, {drawStack.Count} cards remaining");
        
        return card;
    }
    
    /// <summary>
    /// Discard a Supervisor card (for future reshuffle)
    /// </summary>
    public void DiscardCard(GameObject card)
    {
        if (card != null && !discardedCards.Contains(card))
        {
            discardedCards.Add(card);
            card.SetActive(false);
        }
    }
    
    /// <summary>
    /// Get remaining cards in draw stack
    /// </summary>
    public int GetDrawStackCount()
    {
        return drawStack.Count;
    }
    
    /// <summary>
    /// Get discarded cards count
    /// </summary>
    public int GetDiscardCount()
    {
        return discardedCards.Count;
    }
    
    /// <summary>
    /// Force reshuffle (for testing)
    /// </summary>
    public void ForceReshuffle()
    {
        ShuffleDeck();
    }
}