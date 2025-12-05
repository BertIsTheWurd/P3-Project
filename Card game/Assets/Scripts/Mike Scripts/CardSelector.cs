using UnityEngine;

public class CardSelector : MonoBehaviour
{
    public static CardSelector Instance;
    
    private GameObject selectedCard = null;
    private Vector3 originalPosition;
    private Transform originalParent;
    private Vector3 originalScale;
    
    [Header("Visual Feedback")]
    public float selectedCardYOffset = 0.5f; // How high to lift selected card
    public Color selectedCardTint = new Color(1f, 1f, 0.5f, 1f); // Yellow tint
    
    private SpriteRenderer selectedCardRenderer;
    private Color originalColor;
    
    private GameManager gameManager;

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
        if (gameManager == null)
        {
            Debug.LogError("CardSelector: Could not find GameManager!");
        }
    }

    private void Update()
    {
        // Handle mouse clicks
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if we clicked a card in hand
                if (hit.collider.CompareTag("CardInHand"))
                {
                    SelectCard(hit.collider.gameObject);
                }
                // Check if we clicked a grid slot with a card selected
                else if (hit.collider.CompareTag("GridSlot") && selectedCard != null)
                {
                    TryPlaceCard(hit.collider.gameObject);
                }
                // Deselect if we clicked something else
                else if (selectedCard != null)
                {
                    DeselectCard();
                }
            }
        }
    }

    private void SelectCard(GameObject card)
    {
        // Deselect previous card if any
        if (selectedCard != null)
        {
            DeselectCard();
        }

        selectedCard = card;
        
        // Store original state
        originalPosition = card.transform.position;
        originalParent = card.transform.parent;
        originalScale = card.transform.localScale;
        
        // Visual feedback
        selectedCardRenderer = card.GetComponent<SpriteRenderer>();
        if (selectedCardRenderer != null)
        {
            originalColor = selectedCardRenderer.color;
            selectedCardRenderer.color = selectedCardTint;
        }
        
        // Lift card up
        card.transform.position += Vector3.up * selectedCardYOffset;
        
        Debug.Log($"Selected card: {card.GetComponent<Card>().cardData.cardName}");
    }

    private void DeselectCard()
    {
        if (selectedCard == null) return;
        
        // Restore visual state
        if (selectedCardRenderer != null)
        {
            selectedCardRenderer.color = originalColor;
        }
        
        // Return to original position
        selectedCard.transform.position = originalPosition;
        
        Debug.Log("Deselected card");
        
        selectedCard = null;
        selectedCardRenderer = null;
    }

    private void TryPlaceCard(GameObject gridSlot)
    {
        if (selectedCard == null || gameManager == null) return;

        // Find grid position from the grid slot's position
        Vector2Int? gridPos = FindGridPosition(gridSlot.transform.position);
        
        if (gridPos.HasValue)
        {
            Debug.Log($"Attempting to place card at grid position: row {gridPos.Value.y}, col {gridPos.Value.x}");
            
            // Try to play the card
            gameManager.PlayCard(selectedCard, gridPos.Value.x, gridPos.Value.y);
            
            // Card will be deselected by the hand controller if placement succeeds
            // If placement fails, card stays selected
            selectedCard = null;
            selectedCardRenderer = null;
        }
        else
        {
            Debug.LogError("Could not find grid position for clicked slot");
            DeselectCard();
        }
    }

    private Vector2Int? FindGridPosition(Vector3 worldPosition)
    {
        // Search through all card slots to find the closest one
        float minDistance = float.MaxValue;
        Vector2Int? closestPos = null;
        
        for (int row = 0; row < gameManager.cardSlots.GetLength(0); row++)
        {
            for (int col = 0; col < gameManager.cardSlots.GetLength(1); col++)
            {
                float distance = Vector3.Distance(worldPosition, gameManager.cardSlots[row, col]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPos = new Vector2Int(col, row);
                }
            }
        }
        
        // Only return if we found something reasonably close (within 0.5 units)
        if (minDistance < 0.5f)
        {
            return closestPos;
        }
        
        return null;
    }

    // Public method to check if a card is currently selected
    public bool HasSelectedCard()
    {
        return selectedCard != null;
    }

    // Public method to get the selected card
    public GameObject GetSelectedCard()
    {
        return selectedCard;
    }
    
    // Force deselect (called by hand controller when card is played)
    public void ForceDeselect()
    {
        selectedCard = null;
        selectedCardRenderer = null;
    }
}