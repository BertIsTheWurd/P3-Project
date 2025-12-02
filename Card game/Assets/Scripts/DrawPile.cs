
using System.Collections.Generic;
using UnityEngine;

public class DrawPile : MonoBehaviour
{
    public Stack<GameObject> drawPile = new Stack<GameObject>();
    public DiscardPile discardPile;
    public Transform drawAnchor;

    [Header("Visual Settings")] public float cardStackOffsetZ = -0.05f;
    public float cardStackOffsetY = 0.02f;
    public bool showDrawPile = true;

    
    [Header("Card Scale")]
    public Vector3 drawPileCardScale = new Vector3(0.25f, 0.25f, 0.25f);

    public void Initialize(List<GameObject> deck)
    {
        List<GameObject> tempDeck = new List<GameObject>(deck);
        int cardCount = 0;

        while (tempDeck.Count > 0)
        {
            int index = Random.Range(0, tempDeck.Count);
            var card = tempDeck[index];

            card.transform.SetParent(drawAnchor);
            card.transform.localPosition = new Vector3(
                0,
                cardCount * cardStackOffsetY,
                cardCount * cardStackOffsetZ
            );
     
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = drawPileCardScale; // instead of Vector3.one


            card.tag = "DrawPile";

            var cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.UpdateAppearanceBasedOnTag();
            }

            var collider = card.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            card.SetActive(true);
            drawPile.Push(card);
            tempDeck.RemoveAt(index);
            cardCount++;
        }

        Debug.Log($"Draw pile initialized with {drawPile.Count} cards");
    }

    public GameObject DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.Log("Draw pile empty, reshuffling discard pile...");
            drawPile = discardPile.Shuffle();

            int stackIndex = 0;
            foreach (var card in drawPile)
            {
                card.transform.SetParent(drawAnchor);
                card.transform.localPosition = new Vector3(
                    0,
                    stackIndex * cardStackOffsetY,
                    stackIndex * cardStackOffsetZ
                );
                card.transform.localRotation = Quaternion.identity;
                card.tag = "DrawPile";

                var cardComponent = card.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.UpdateAppearanceBasedOnTag();
                }

                var collider = card.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;

                card.SetActive(true);
                stackIndex++;
            }

            Debug.Log($"Reshuffled {drawPile.Count} cards into draw pile");
        }

        if (drawPile.Count > 0)
        {
            var card = drawPile.Pop();

            var collider = card.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            card.tag = "CardInHand";

            var cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.UpdateAppearanceBasedOnTag();
            }

            Debug.Log($"Drew card: {card.GetComponent<Card>().cardData.cardName}, {drawPile.Count} cards remaining");
            return card;
        }

        Debug.LogWarning("No cards available to draw!");
        return null;
    }

    public int CardCount => drawPile.Count;
}