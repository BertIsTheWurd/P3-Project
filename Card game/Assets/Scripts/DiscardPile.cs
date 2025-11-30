using System.Collections.Generic;
using UnityEngine;

public class DiscardPile : MonoBehaviour {
    public List<GameObject> discardPile = new List<GameObject>();
    public Transform discardAnchor; // Physical representation in scene
    
    [Header("Visual Settings")]
    public float cardStackOffset = 0.02f;
    public Vector3 discardPileScale = Vector3.one;
    public Vector3 discardPileRotation = new Vector3(90, 0, 0); // Flat on table

    public void AddToDiscard(GameObject card) {
        card.SetActive(true);
        card.transform.SetParent(discardAnchor);
        card.transform.localPosition = new Vector3(0, discardPile.Count * cardStackOffset, 0);
        card.transform.localRotation = Quaternion.Euler(discardPileRotation);
        card.transform.localScale = discardPileScale;
        card.tag = "DiscardPile";
        
        // Show card front in discard pile
        card.GetComponent<Card>().ShowCardFront();
        
        // Disable collider so cards can't be selected from discard
        var collider = card.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        
        discardPile.Add(card);
        Debug.Log($"Card added to discard pile. Discard size: {discardPile.Count}");
    }

    public Stack<GameObject> Shuffle() {
        Stack<GameObject> stack = new Stack<GameObject>();
        int c = discardPile.Count;
        
        while (c > 0) {
            int index = Random.Range(0, c);
            var card = discardPile[index];
            discardPile.RemoveAt(index);
            stack.Push(card);
            c--;
        }
        
        Debug.Log($"Shuffled {stack.Count} cards from discard pile");
        return stack;
    }
    
    // Get count for UI display
    public int CardCount => discardPile.Count;
}