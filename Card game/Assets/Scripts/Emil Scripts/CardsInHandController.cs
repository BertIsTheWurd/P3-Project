using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class CardsInHandController : MonoBehaviour
{
    // Event to inform others (like GameManager) when the selection changes
    public static event Action<int> OnCardSelected;

    // The actual GameObjects representing the cards the player is holding.
    [SerializeField] private List<GameObject> cardObjectsInHand = new List<GameObject>();

    // The index of the card currently selected by the player
    [SerializeField] private int currentCardIndex = 0;

    // The parent transform where cards will be anchored in 3D space.
    // This is the same object whose transform is updated by CameraSwitcher.
    public Transform handPlaceHolder;

    [SerializeField] private float cardSpacing = 0.5f;
    
    private GameManager gameManager;
    private int maxHandsize;

    void Start()
    {
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

    void Update()
    {
        // Only allow card cycling if not in FPS mode (We'll subscribe to an event for this later)
        // For now, let's assume we can always cycle cards, or use A/D keys
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            if (currentCardIndex < cardObjectsInHand.Count - 1)
            {
                currentCardIndex++;
                ActivateCard(currentCardIndex);
            }
        }

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            if (currentCardIndex > 0)
            {
                currentCardIndex--;
                ActivateCard(currentCardIndex);
            }
        }

    }

    // --- PUBLIC METHODS (GameManager will use these) ---

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
    }
}

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    