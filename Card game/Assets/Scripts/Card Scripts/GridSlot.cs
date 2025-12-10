using UnityEngine;

public class GridSlot : MonoBehaviour {
    [Header("Visual Settings")]
    public Sprite emptySlotSprite;
    public Color normalColor = new Color(1f, 1f, 1f, 0.3f); // Semi-transparent
    public Color hoverColor = new Color(0.5f, 1f, 0.5f, 0.6f); // Green tint on hover
    public Color invalidColor = new Color(1f, 0.5f, 0.5f, 0.6f); // Red tint for invalid
    
    private SpriteRenderer spriteRenderer;
    private bool isHovered = false;
    
    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (emptySlotSprite != null) {
            spriteRenderer.sprite = emptySlotSprite;
        }
        
        spriteRenderer.color = normalColor;
        
        // Rotate to be flat on table
        transform.rotation = Quaternion.Euler(90, 0, 0);
    }
    
    void OnMouseEnter() {
        isHovered = true;
        spriteRenderer.color = hoverColor;
    }
    
    void OnMouseExit() {
        isHovered = false;
        spriteRenderer.color = normalColor;
    }
    
    public void SetValid(bool valid) {
        if (isHovered) {
            spriteRenderer.color = valid ? hoverColor : invalidColor;
        }
    }
    
    public void ShowAsOccupied() {
        spriteRenderer.enabled = false; // Hide slot when occupied
    }
    
    public void ShowAsEmpty() {
        spriteRenderer.enabled = true; // Show slot when empty
    }
}