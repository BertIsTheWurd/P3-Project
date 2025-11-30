using System.Collections.Generic;
using UnityEngine;

public class DrawPile : MonoBehaviour {
    public Stack<GameObject> drawPile = new Stack<GameObject>();
    public DiscardPile discardPile;
    public Transform drawAnchor; // Visual representation
    
    [Header("Visual Settings")]
    public float cardStackOffset = 0.02f; // Small offset for visual stacking
    public bool showDrawPile = true; // Toggle to show/hide draw pile cards

    public void Initialize(List<GameObject> deck) {
        // Shuffle initial deck
        List<GameObject> tempDeck = new List<GameObject>(deck);
        int cardCount = 0;
        
        while (tempDeck.Count > 0) {
            int index = Random.Range(0, tempDeck.Count);
            var card = tempDeck[index];
            
            // Set up card for draw pile
            card.transform.SetParent(drawAnchor);
            card.transform.localPosition = new Vector3(0, cardCount * cardStackOffset, 0);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = Vector3.one;
            card.tag = "DrawPile"; // Prevent clicking
            
            // Disable collider so cards can't be selected from draw pile
            var collider = card.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            
            // Always show draw pile cards
            card.SetActive(true);
            
            drawPile.Push(card);
            tempDeck.RemoveAt(index);
            cardCount++;
        }
        
        Debug.Log($"Draw pile initialized with {drawPile.Count} cards");
    }

    public GameObject DrawCard() {
        // If draw pile is empty, reshuffle discard pile
        if (drawPile.Count == 0) {
            Debug.Log("Draw pile empty, reshuffling discard pile...");
            drawPile = discardPile.Shuffle();
            
            // Re-parent all cards to draw anchor
            int stackIndex = 0;
            foreach (var card in drawPile) {
                card.transform.SetParent(drawAnchor);
                card.transform.localPosition = new Vector3(0, stackIndex * cardStackOffset, 0);
                card.transform.localRotation = Quaternion.identity;
                card.tag = "DrawPile";
                
                // Disable collider
                var collider = card.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                
                card.SetActive(true);
                stackIndex++;
            }
            
            Debug.Log($"Reshuffled {drawPile.Count} cards into draw pile");
        }

        if (drawPile.Count > 0) {
            var card = drawPile.Pop();
            
            // Re-enable collider for hand
            var collider = card.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;
            
            card.tag = "CardInHand"; // Change tag for hand
            
            Debug.Log($"Drew card: {card.GetComponent<Card>().cardData.cardName}, {drawPile.Count} cards remaining");
            return card;
        }
        
        Debug.LogWarning("No cards available to draw!");
        return null;
    }

    // Get count for UI display
    public int CardCount => drawPile.Count;
}