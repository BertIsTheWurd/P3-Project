using System.Collections.Generic;
using UnityEngine;

public class DiscardPile : MonoBehaviour {
    public List<GameObject> discardPile = new List<GameObject>();
    public Transform discardAnchor; // Physical representation in scene
    
    [Header("Visual Settings")]
    public float cardStackOffset = 0.02f;
    public Vector3 discardPileScale = Vector3.one;
    public Vector3 discardPileRotation = new Vector3(90, 0, 0); // Flat on table
    
    [Header("Discard Pile Base")]
    [Tooltip("Sprite to show as the discard pile base (where players click to discard)")]
    public Sprite discardPileBaseSprite;
    [Tooltip("Scale of the base sprite")]
    public Vector3 baseScale = new Vector3(0.25f, 0.25f, 0.25f);
    
    private GameObject discardPileBase;

    private void Start() {
        CreateDiscardPileBase();
    }
    
    /// <summary>
    /// Create the discard pile base sprite that players can click on to discard cards
    /// </summary>
    private void CreateDiscardPileBase() {
        if (discardAnchor == null) {
            Debug.LogError("DiscardPile: discardAnchor not assigned!");
            return;
        }
        
        // Create base object
        discardPileBase = new GameObject("DiscardPileBase");
        discardPileBase.transform.SetParent(discardAnchor);
        discardPileBase.transform.localPosition = Vector3.zero;
        discardPileBase.transform.localRotation = Quaternion.Euler(discardPileRotation);
        discardPileBase.transform.localScale = baseScale;
        discardPileBase.tag = "DiscardPileBase";
        
        // Add sprite renderer
        SpriteRenderer spriteRenderer = discardPileBase.AddComponent<SpriteRenderer>();
        if (discardPileBaseSprite != null) {
            spriteRenderer.sprite = discardPileBaseSprite;
        }
        spriteRenderer.sortingOrder = -1; // Behind cards
        
        // Add collider for clicking
        BoxCollider collider = discardPileBase.AddComponent<BoxCollider>();
        // Size the collider based on the sprite bounds or a default size
        if (discardPileBaseSprite != null) {
            collider.size = new Vector3(
                discardPileBaseSprite.bounds.size.x,
                discardPileBaseSprite.bounds.size.y,
                0.1f
            );
        } else {
            collider.size = new Vector3(2f, 3f, 0.1f); // Default card-ish size
        }
        
        Debug.Log("Discard pile base created");
    }

    public void AddToDiscard(GameObject card) {
        card.SetActive(true);
        card.transform.SetParent(discardAnchor);
        // Stack cards slightly above the base
        card.transform.localPosition = new Vector3(0, (discardPile.Count + 1) * cardStackOffset, 0);
        card.transform.localRotation = Quaternion.Euler(discardPileRotation);
        card.transform.localScale = discardPileScale;
        card.tag = "DiscardPile";
        
        // Reset sprite color to white (remove any tints from selection, etc.)
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.color = Color.white;
        }
        
        // Show card front in discard pile
        var cardComponent = card.GetComponent<Card>();
        if (cardComponent != null) {
            cardComponent.ShowCardFront();
            cardComponent.ResetRotation();
        }
        
        // Disable collider so cards can't be selected from discard
        // But keep it enabled so we can click to discard more cards
        var collider = card.GetComponent<Collider>();
        if (collider != null) collider.enabled = true;
        
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