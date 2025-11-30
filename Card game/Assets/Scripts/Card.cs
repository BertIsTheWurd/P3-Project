using UnityEngine;

public class Card : MonoBehaviour {
    public DirectionalCardData cardData;
    private SpriteRenderer spriteRenderer;
    
    [Header("Card Back")]
    public Sprite cardBackSprite; // Assign in prefab inspector

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() {
        UpdateSprite();
    }

    // Call this whenever cardData changes
    public void SetCardData(DirectionalCardData data) {
        cardData = data;
        UpdateSprite();
    }

    private void UpdateSprite() {
        if (cardData != null && spriteRenderer != null) {
            spriteRenderer.sprite = cardData.cardImage;
        }
    }
    
    // Show the card back (for draw pile)
    public void ShowCardBack() {
        if (spriteRenderer != null && cardBackSprite != null) {
            spriteRenderer.sprite = cardBackSprite;
        }
    }
    
    // Show the card front
    public void ShowCardFront() {
        UpdateSprite();
    }

    public bool ConnectsUp => cardData.connectsUp;
    public bool ConnectsDown => cardData.connectsDown;
    public bool ConnectsLeft => cardData.connectsLeft;
    public bool ConnectsRight => cardData.connectsRight;
}