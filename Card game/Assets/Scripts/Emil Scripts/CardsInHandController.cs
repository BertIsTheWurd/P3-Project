using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class CardsInHandController : MonoBehaviour {
    public static event Action<int> OnCardSelected;

    [SerializeField] private List<GameObject> cardObjectsInHand = new List<GameObject>();
    [SerializeField] private int currentCardIndex = -1; // -1 means none selected
    public Transform handPlaceHolder;

    [Header("Hand Visual Settings")]
    public float cardSpacing = 0.3f;
    public float fanRadius = 5f;
    public float fanAngle = 30f;
    public Vector3 handCardScale = Vector3.one;
    public Vector3 handCardRotation = Vector3.zero;

    [Header("Selection Visual")]
    public float selectedCardYOffset = 0.3f;
    public float selectedCardScaleMultiplier = 1.2f;
    public Color selectedCardTint = Color.yellow;
    public float selectionAnimationSpeed = 10f;

    private Camera mainCamera;
    private GameManager gameManager;
    private GameObject currentlySelectedCard;
    private Color originalCardColor = Color.white;
    private Vector3 targetPosition;
    private Vector3 targetScale;
    private bool isAnimatingSelection = false;

    public int HandCount => cardObjectsInHand.Count;

	//Shoddy merge patchwork
    [SerializeField] private float cardSpacing = 0.5f;
    
    private GameManager gameManager;
    private int maxHandsize;

    void Start()
    {
    	mainCamera = Camera.main;
        gameManager = FindObjectOfType<GameManager>();
        
        if (handPlaceHolder == null) {
            Debug.LogError("Hand PlaceHolder not assigned!");
        // find and store the GameManager component.
          gameManager = FindObjectOfType<GameManager>();
        // check if the GameManager was found before using its property.
        if (gameManager != null)
        {
             maxHandsize = gameManager.HandSize;
        }

        if (cardObjectsInHand.Count > 0)
        {
            ActivateCard(currentCardIndex);
            UpdateCardVisuals();
        }
    }

    void Update() {
        HandleCardSelection();
        HandleCardRotation();
        HandleGridPlacement();
        if (isAnimatingSelection) {
            AnimateSelectedCard();
        }
    }

    private void HandleCardSelection() {
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                if (hit.collider.CompareTag("CardInHand")) {
                    int index = cardObjectsInHand.IndexOf(hit.collider.gameObject);
                    if (index >= 0) {
                        SelectCard(index);
                    }
                }
                else if (hit.collider.CompareTag("GridSlot")) {
                    // Don't deselect when clicking grid - let HandleGridPlacement handle it
                }
                else {
                    // Deselect if clicking elsewhere
                    DeselectCard();
                }
            }
        }
    }

    private void SelectCard(int index) {
        // If clicking the same card, deselect it
        if (currentCardIndex == index) {
            DeselectCard();
            return;
        }

        // Deselect previous card
        if (currentlySelectedCard != null && currentCardIndex >= 0 && currentCardIndex < cardObjectsInHand.Count) {
            ResetCardVisuals(currentCardIndex);
        }

        currentCardIndex = index;
        currentlySelectedCard = cardObjectsInHand[currentCardIndex];
        
        // Set up animation targets
        targetPosition = CalculateCardPosition(currentCardIndex);
        targetPosition.y += selectedCardYOffset;
        targetScale = handCardScale * selectedCardScaleMultiplier;
        isAnimatingSelection = true;
        
        OnCardSelected?.Invoke(currentCardIndex);
        Debug.Log($"Selected card: {currentlySelectedCard.GetComponent<Card>().cardData.cardName}");
    }

    private void DeselectCard() {
        if (currentCardIndex < 0 || currentlySelectedCard == null) return;
        
        ResetCardVisuals(currentCardIndex);
        currentCardIndex = -1;
        currentlySelectedCard = null;
        isAnimatingSelection = false;
        
        Debug.Log("Deselected card");
    }

    private void AnimateSelectedCard() {
        if (currentlySelectedCard == null || currentCardIndex < 0) {
            isAnimatingSelection = false;
            return;
        }

        // Smoothly animate to target
        currentlySelectedCard.transform.localPosition = Vector3.Lerp(
            currentlySelectedCard.transform.localPosition,
            targetPosition,
            Time.deltaTime * selectionAnimationSpeed
        );
        
        currentlySelectedCard.transform.localScale = Vector3.Lerp(
            currentlySelectedCard.transform.localScale,
            targetScale,
            Time.deltaTime * selectionAnimationSpeed
        );
        
        // Apply tint
        var spriteRenderer = currentlySelectedCard.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.color = selectedCardTint;
        }
        
        // Check if animation is complete
        if (Vector3.Distance(currentlySelectedCard.transform.localPosition, targetPosition) < 0.01f) {
            isAnimatingSelection = false;
        }
    }

    private void ResetCardVisuals(int index) {
        if (index < 0 || index >= cardObjectsInHand.Count) return;
        
        var card = cardObjectsInHand[index];
        
        // Reset position, rotation, and scale
        card.transform.localPosition = CalculateCardPosition(index);
        card.transform.localRotation = CalculateCardRotation(index);
        card.transform.localScale = handCardScale;
        
        // Reset color
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.color = originalCardColor;
        }
    }

    private void HandleGridPlacement() {
        if (currentCardIndex == -1 || currentlySelectedCard == null) return;

        // Use left click to place card (since left click selects)
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                if (hit.collider.CompareTag("GridSlot")) {
                    TryPlaceCardOnGrid(hit.collider.gameObject);
                }
            }
        }
    }
    
    private void HandleCardRotation() {
        // Only rotate if a card is selected
        if (currentCardIndex == -1 || currentlySelectedCard == null) return;
        
        // Check for R key press (works with both old and new input system)
        if (Input.GetKeyDown(KeyCode.R)) {
            var card = currentlySelectedCard.GetComponent<Card>();
            if (card != null) {
                card.Rotate180();
                Debug.Log($"Rotated card: {card.cardData.cardName} to {card.GetRotation()} degrees");
            }
        }
    }

    private void TryPlaceCardOnGrid(GameObject gridSlot) {
        Vector3 slotPos = gridSlot.transform.position;

        // Find grid coordinates - using the correct array dimensions
        int col = -1, row = -1;
        float minDistance = float.MaxValue;
        
        for (int r = 0; r < gameManager.cardSlots.GetLength(0); r++) {
            for (int c = 0; c < gameManager.cardSlots.GetLength(1); c++) {
                float distance = Vector3.Distance(gameManager.cardSlots[r, c], slotPos);
                if (distance < minDistance) {
                    minDistance = distance;
                    row = r;
                    col = c;
                }
            }
        }

        // Only try to place if we found a close match
        if (minDistance < 0.5f && col >= 0 && row >= 0) {
            var selectedCard = cardObjectsInHand[currentCardIndex];
            
            // Check if placement is valid - pass GameObject so rotation is considered
            if (gameManager.CanPlaceCard(col, row, selectedCard)) {
                // Place the card
                gameManager.PlayCard(selectedCard, col, row);
                
                // Deselect
                currentCardIndex = -1;
                currentlySelectedCard = null;
                isAnimatingSelection = false;
            } else {
                Debug.Log("Cannot place card at this location");
            }
        }
    }

    public void AddCardToHand(GameObject card) {
        if (card == null) {
            Debug.LogError("Trying to add null card to hand!");
            return;
        }

        cardObjectsInHand.Add(card);
        card.SetActive(true);
        card.tag = "CardInHand";
        
        // Position card visually in hand
        card.transform.SetParent(handPlaceHolder);
        
        // Reset rotation when adding to hand
        var cardComponent = card.GetComponent<Card>();
        if (cardComponent != null) {
            cardComponent.ResetRotation();
        }
        
        // Enable collider for selection
        var collider = card.GetComponent<Collider>();
        if (collider != null) collider.enabled = true;
        
        // Reset sprite color to original
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.color = originalCardColor;
        }
        
        RepositionAllCards();
        
        Debug.Log($"Added {card.GetComponent<Card>().cardData.cardName} to hand. Hand size: {HandCount}");
    }

    public void RemoveCardFromHand(GameObject card) {
        if (cardObjectsInHand.Contains(card)) {
            int removedIndex = cardObjectsInHand.IndexOf(card);
            cardObjectsInHand.Remove(card);
            
            // If we removed the selected card or a card before it, adjust selection
            if (currentCardIndex == removedIndex) {
                currentCardIndex = -1;
                currentlySelectedCard = null;
                isAnimatingSelection = false;
            } else if (currentCardIndex > removedIndex) {
                currentCardIndex--;
            }
            
            RepositionAllCards();
            
            Debug.Log($"Removed card from hand. Hand size: {HandCount}");
        }
    }

    private void RepositionAllCards() {
        for (int i = 0; i < cardObjectsInHand.Count; i++) {
            // Don't reposition if it's the currently selected card (it's animating)
            if (i != currentCardIndex || !isAnimatingSelection) {
                RepositionCard(i);
            }
        }
    }

    private void RepositionCard(int index) {
        if (index < 0 || index >= cardObjectsInHand.Count) return;
        
        var card = cardObjectsInHand[index];
        card.transform.localPosition = CalculateCardPosition(index);
        card.transform.localRotation = CalculateCardRotation(index);
        card.transform.localScale = handCardScale;
    }

    // This method handles the visibility and selection state
    private void ActivateCard(int index)
    {
        // Safety check to ensure the index is valid (not empty, not out of bounds).
        if (cardObjectsInHand.Count == 0 || index < 0 || index >= cardObjectsInHand.Count) return;

        for (int i = 0; i < cardObjectsInHand.Count; i++)
        {
            if (cardObjectsInHand[i] != null)
            {
                // Determine the target Z position based on whether the card is selected.
                // -1f moves the card forward (hover effect). 0f is the base position set by UpdateCardVisuals.
                float targetZ = (i == index) ? -1f : 0f; 

                // We only modify the Z component to apply the 'hover' effect. 
                // X and Y positions remain the same, preserving the hand arrangement.
                cardObjectsInHand[i].transform.localPosition = new Vector3(
                    cardObjectsInHand[i].transform.localPosition.x,
                    cardObjectsInHand[i].transform.localPosition.y,
                    targetZ
                );
            }
        }
        
        // EVENT BROADCAST: Tell the GameManager of the selection change.
        // OnCardSelected?.Invoke(index) triggers any method that has subscribed to this event,
        // sending the 'index' integer as the message payload.
        OnCardSelected?.Invoke(index);
    }

    // Sets the base position for all cards in the hand
    private void UpdateCardVisuals()
    {
        float totalWidth = (cardObjectsInHand.Count - 1) * cardSpacing;
        
        // Calculate the starting position to center the hand on the placeholder
        float startX = -totalWidth / 2f;

        for (int i = 0; i < cardObjectsInHand.Count; i++)
        {
            if (cardObjectsInHand[i] != null)
            {
                cardObjectsInHand[i].SetActive(true);

                // Calculate position
                Vector3 position = new Vector3(startX + (i * cardSpacing), 0f, 0f); // Spread horizontally
                
                cardObjectsInHand[i].transform.SetParent(handPlaceHolder, false);
                cardObjectsInHand[i].transform.localPosition = position;
                cardObjectsInHand[i].transform.localRotation = Quaternion.identity;
            }
	}
    private Vector3 CalculateCardPosition(int index) {
        int totalCards = cardObjectsInHand.Count;
        
        if (totalCards == 1) {
            return Vector3.zero;
        }
        
        // Calculate position in arc
        float normalizedIndex = totalCards > 1 ? (float)index / (totalCards - 1) : 0.5f;
        float angle = Mathf.Lerp(-fanAngle / 2f, fanAngle / 2f, normalizedIndex);
        float angleRad = angle * Mathf.Deg2Rad;
        
        // Position on arc
        float x = Mathf.Sin(angleRad) * fanRadius;
        float y = -fanRadius + Mathf.Cos(angleRad) * fanRadius;
        
        return new Vector3(x, y, 0);
    }

    private Quaternion CalculateCardRotation(int index) {
        int totalCards = cardObjectsInHand.Count;
        
        if (totalCards == 1) {
            return Quaternion.Euler(handCardRotation);
        }
        
        // Calculate rotation for fan effect
        float normalizedIndex = totalCards > 1 ? (float)index / (totalCards - 1) : 0.5f;
        float angle = Mathf.Lerp(-fanAngle / 2f, fanAngle / 2f, normalizedIndex);
        
        return Quaternion.Euler(handCardRotation.x, handCardRotation.y, handCardRotation.z + angle);
    }
    
    // Public method to check if a card is selected
    public bool HasSelectedCard() {
        return currentCardIndex >= 0 && currentlySelectedCard != null;
    }
    
    // Public method to get selected card
    public GameObject GetSelectedCard() {
        return currentlySelectedCard;
    }
}