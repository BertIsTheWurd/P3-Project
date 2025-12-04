using UnityEngine;

public class Card : MonoBehaviour
{
    public DirectionalCardData cardData;
    private SpriteRenderer spriteRenderer;
    
    // Rotation state (0, 90, 180, 270 degrees)
    private int currentRotation = 0;
    
    // Store original connections from card data
    private bool originalConnectsUp;
    private bool originalConnectsDown;
    private bool originalConnectsLeft;
    private bool originalConnectsRight;

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
        
        // Store original connections
        if (cardData != null)
        {
            originalConnectsUp = cardData.connectsUp;
            originalConnectsDown = cardData.connectsDown;
            originalConnectsLeft = cardData.connectsLeft;
            originalConnectsRight = cardData.connectsRight;
        }
        
        // Reset rotation when new card data is set
        currentRotation = 0;
        
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
    
    // Rotate the card 180 degrees
    public void Rotate180()
    {
        currentRotation = (currentRotation + 180) % 360;
        
        // Set absolute local rotation (don't add to existing rotation)
        // This prevents accumulation of rotation from hand positioning
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, currentRotation);
        
        Debug.Log($"Card rotated to {currentRotation} degrees");
    }
    
    // Reset rotation to 0
    public void ResetRotation()
    {
        if (currentRotation == 0) return;
        
        currentRotation = 0;
        
        // Set absolute local rotation to 0
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, 0);
        
        Debug.Log($"Card rotation reset to 0 degrees");
    }
    
    // Get current rotation
    public int GetRotation()
    {
        return currentRotation;
    }

    // Connection properties that account for rotation
    public bool ConnectsUp
    {
        get
        {
            switch (currentRotation)
            {
                case 0:   return originalConnectsUp;
                case 90:  return originalConnectsLeft;
                case 180: return originalConnectsDown;
                case 270: return originalConnectsRight;
                default:  return originalConnectsUp;
            }
        }
    }

    public bool ConnectsDown
    {
        get
        {
            switch (currentRotation)
            {
                case 0:   return originalConnectsDown;
                case 90:  return originalConnectsRight;
                case 180: return originalConnectsUp;
                case 270: return originalConnectsLeft;
                default:  return originalConnectsDown;
            }
        }
    }

    public bool ConnectsLeft
    {
        get
        {
            switch (currentRotation)
            {
                case 0:   return originalConnectsLeft;
                case 90:  return originalConnectsDown;
                case 180: return originalConnectsRight;
                case 270: return originalConnectsUp;
                default:  return originalConnectsLeft;
            }
        }
    }

    public bool ConnectsRight
    {
        get
        {
            switch (currentRotation)
            {
                case 0:   return originalConnectsRight;
                case 90:  return originalConnectsUp;
                case 180: return originalConnectsLeft;
                case 270: return originalConnectsDown;
                default:  return originalConnectsRight;
            }
        }
    }
}