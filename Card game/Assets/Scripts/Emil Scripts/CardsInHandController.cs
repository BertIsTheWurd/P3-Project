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
    public float cardSpacing = 0.3f; // Horizontal spacing between cards
    public float fanRadius = 5f; // Radius of the arc
    public float fanAngle = 30f; // Total angle spread of the fan (degrees)
    public Vector3 handCardScale = Vector3.one; // Scale of cards in hand
    public Vector3 handCardRotation = Vector3.zero; // Base rotation for cards in hand

    [Header("Selection Visual")]
    public float selectedCardYOffset = 0.3f; // How much to move selected card forward
    public float selectedCardScaleMultiplier = 1.2f; // Scale up selected card
    public Color selectedCardTint = Color.yellow; // Tint for selected card
    public float selectionAnimationSpeed = 10f; // Animation speed

    private Camera mainCamera;
    private GameManager gameManager;
    private GameObject currentlySelectedCard;
    private Color originalCardColor = Color.white;

    public int HandCount => cardObjectsInHand.Count;

    void Start() {
        mainCamera = Camera.main;
        gameManager = FindObjectOfType<GameManager>();
        
        if (handPlaceHolder == null) {
            Debug.LogError("Hand PlaceHolder not assigned!");
        }
    }

    void Update() {
        HandleCardSelection();
        HandleGridPlacement();
        AnimateSelectedCard();
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
            }
        }
    }

    private void SelectCard(int index) {
        // Deselect previous card
        if (currentlySelectedCard != null && currentCardIndex >= 0 && currentCardIndex < cardObjectsInHand.Count) {
            ResetCardVisuals(currentCardIndex);
        }

        currentCardIndex = index;
        currentlySelectedCard = cardObjectsInHand[currentCardIndex];
        
        OnCardSelected?.Invoke(currentCardIndex);
        Debug.Log($"Selected card: {currentlySelectedCard.GetComponent<Card>().cardData.cardName}");
    }

    private void AnimateSelectedCard() {
        if (currentlySelectedCard == null || currentCardIndex < 0) return;

        // Get target position for selected card
        Vector3 targetPos = CalculateCardPosition(currentCardIndex);
        targetPos.y += selectedCardYOffset; // Move forward
        
        Vector3 targetScale = handCardScale * selectedCardScaleMultiplier;
        
        // Smoothly animate to target
        currentlySelectedCard.transform.localPosition = Vector3.Lerp(
            currentlySelectedCard.transform.localPosition,
            targetPos,
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
    }

    private void ResetCardVisuals(int index) {
        if (index < 0 || index >= cardObjectsInHand.Count) return;
        
        var card = cardObjectsInHand[index];
        RepositionCard(index);
        
        // Reset color
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.color = originalCardColor;
        }
    }

    private void HandleGridPlacement() {
        if (currentCardIndex == -1) return; // No card selected

        if (Mouse.current.rightButton.wasPressedThisFrame) {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                if (hit.collider.CompareTag("GridSlot")) {
                    Vector3 slotPos = hit.collider.transform.position;

                    // Find grid coordinates
                    int x = -1, z = -1;
                    for (int i = 0; i < gameManager.gridZ; i++) {
                        for (int j = 0; j < gameManager.gridX; j++) {
                            if (Vector3.Distance(gameManager.cardSlots[i, j], slotPos) < 0.5f) {
                                x = j; z = i;
                                break;
                            }
                        }
                        if (x >= 0) break;
                    }

                    if (x >= 0 && z >= 0) {
                        var selectedCard = cardObjectsInHand[currentCardIndex];
                        gameManager.PlayCard(selectedCard, x, z);
                        currentCardIndex = -1;
                        currentlySelectedCard = null;
                    }
                }
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
        card.tag = "CardInHand"; // Ensure correct tag
        
        // Position card visually in hand
        card.transform.SetParent(handPlaceHolder);
        
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
            cardObjectsInHand.Remove(card);
            RepositionAllCards();
            
            Debug.Log($"Removed card from hand. Hand size: {HandCount}");
        }
    }

    private void RepositionAllCards() {
        for (int i = 0; i < cardObjectsInHand.Count; i++) {
            RepositionCard(i);
        }
    }

    private void RepositionCard(int index) {
        if (index < 0 || index >= cardObjectsInHand.Count) return;
        
        var card = cardObjectsInHand[index];
        card.transform.localPosition = CalculateCardPosition(index);
        card.transform.localRotation = CalculateCardRotation(index);
        card.transform.localScale = handCardScale;
    }

    private Vector3 CalculateCardPosition(int index) {
        int totalCards = cardObjectsInHand.Count;
        
        if (totalCards == 1) {
            // Single card centered
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
}