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
    
    // Card state flags
    private bool isBlocked = false;      // Card is blocked by Dead End, Bureaucratic Barrier, etc.
    private bool isDisabled = false;     // Card is disabled by Censor
    private bool isFaceDown = false;     // Card is face-down (for end cards before Peek)
    private CardType blockingCardType;   // What type of card is blocking this?
    
    // Public property for censored state
    public bool isCensored
    {
        get { return isDisabled; }
        set { isDisabled = value; }
    }

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
            // Show card back if face-down, otherwise show front
            if (isFaceDown && cardData.cardBackside != null)
            {
                spriteRenderer.sprite = cardData.cardBackside;
                
                // WORKAROUND: Compensate for different sprite dimensions
                // If backside sprite has different Pixels Per Unit, adjust scale
                if (cardData.cardImage != null)
                {
                    float frontPPU = cardData.cardImage.pixelsPerUnit;
                    float backPPU = cardData.cardBackside.pixelsPerUnit;
                    
                    // Calculate scale compensation
                    float scaleCompensation = backPPU / frontPPU;
                    
                    // Apply compensated scale (base scale 0.25f * compensation)
                    float finalScale = 0.25f * scaleCompensation;
                    transform.localScale = new Vector3(finalScale, finalScale, finalScale);
                    
                    Debug.Log($"Card back scale compensation: {scaleCompensation} (final scale: {finalScale})");
                }
            }
            else
            {
                spriteRenderer.sprite = cardData.cardImage;
                
                // Reset to standard grid scale
                transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                
                // Apply card tint if specified
                if (cardData.cardTint != Color.white)
                {
                    spriteRenderer.color = cardData.cardTint;
                }
            }
        }
    }

    // Show the card back (for draw pile or face-down end cards)
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
            
        // Visual feedback for disabled/blocked cards
        UpdateVisualState();
    }
    
    // Update visual state based on card status
    private void UpdateVisualState()
    {
        if (spriteRenderer == null) return;
        
        if (isDisabled)
        {
            // Darken and desaturate disabled cards
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
        else if (isBlocked)
        {
            // Slightly red tint for blocked cards
            spriteRenderer.color = new Color(1f, 0.7f, 0.7f, 1f);
        }
    }
    
    // Face-down state management (for end cards)
    public void SetFaceDown(bool faceDown)
    {
        isFaceDown = faceDown;
        UpdateSprite();
        Debug.Log($"Card {cardData.cardName} face-down state set to: {faceDown}");
    }
    
    public bool IsFaceDown()
    {
        return isFaceDown;
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
    
    // Blocking state management
    public void SetBlocked(bool blocked, CardType blockType = CardType.DeadEnd)
    {
        isBlocked = blocked;
        blockingCardType = blockType;
        UpdateVisualState();
        Debug.Log($"Card {cardData.cardName} blocked state set to: {blocked}");
    }
    
    public bool IsBlocked()
    {
        return isBlocked;
    }
    
    public CardType GetBlockingCardType()
    {
        return blockingCardType;
    }
    
    // Disabled state management (for Censor cards)
    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
        UpdateVisualState();
        Debug.Log($"Card {cardData.cardName} disabled state set to: {disabled}");
    }
    
    public bool IsDisabled()
    {
        return isDisabled;
    }

    // Connection properties that account for rotation AND blocking/disabled state
    public bool ConnectsUp
    {
        get
        {
            if (isDisabled || isBlocked) return false;  // Disabled/blocked cards don't connect
            
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
            if (isDisabled || isBlocked) return false;
            
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
            if (isDisabled || isBlocked) return false;
            
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
            if (isDisabled || isBlocked) return false;
            
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