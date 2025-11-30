using UnityEngine;
using System.Collections.Generic;

public class CardPool : MonoBehaviour {
    public GameObject cardPrefab;
    public DeckDefinition deckDefinition; // Only regular playable cards
    public List<GameObject> cards = new List<GameObject>();

    private void Start() {
        // Create card pool from deck definition
        for (int i = 0; i < deckDefinition.cards.Count; i++) {
            for (int j = 0; j < deckDefinition.quantities[i]; j++) {
                GameObject temp = Instantiate(cardPrefab, transform);
                temp.GetComponent<Card>().SetCardData(deckDefinition.cards[i]); // Use SetCardData!
                temp.tag = "CardInHand";
                temp.SetActive(false);
                cards.Add(temp);
            }
        }
    }

    // Get any available card from pool
    public GameObject ReturnCard() {
        foreach (var card in cards) {
            if (!card.activeInHierarchy) return card;
        }
        throw new System.Exception("No Card Available in Pool");
    }

    // Create a special card (for start/end cards) that doesn't come from the pool
    public GameObject CreateSpecialCard(DirectionalCardData cardData) {
        GameObject specialCard = Instantiate(cardPrefab, transform);
        specialCard.GetComponent<Card>().SetCardData(cardData); // Use SetCardData!
        specialCard.tag = "PlayedCard"; // Different tag
        return specialCard;
    }
}