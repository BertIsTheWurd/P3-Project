using UnityEngine;

public class Card : MonoBehaviour
{
    public DirectionalCardData cardData;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        UpdateAppearanceBasedOnTag();
    }

    // Call this whenever cardData changes
    public void SetCardData(DirectionalCardData data)
    {
        cardData = data;
        UpdateAppearanceBasedOnTag();
    }

    private void UpdateSprite()
    {
        if (cardData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = cardData.cardImage;
        }
    }

    // Show the card back (for draw pile)
    public void ShowCardBack()
    {
        if (spriteRenderer != null && cardData != null && cardData.cardBackside != null)
        {
            spriteRenderer.sprite = cardData.cardBackside;
        }
    }

    // Show the card front
    public void ShowCardFront()
    {
        UpdateSprite();
    }

    // Dynamic appearance based on tag
    public void UpdateAppearanceBasedOnTag()
    {
        if (tag == "DrawPile")
            ShowCardBack();
        else
            ShowCardFront();
    }

    public bool ConnectsUp => cardData.connectsUp;
    public bool ConnectsDown => cardData.connectsDown;
    public bool ConnectsLeft => cardData.connectsLeft;
    public bool ConnectsRight => cardData.connectsRight;
}