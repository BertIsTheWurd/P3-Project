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

    private GameObject gameManager;
    private int maxHandsize;

    void Start()
    {
        // Finds the GameManager instance to potentially access hand data and size.
        //  gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null) ;
        {
            // maxHandsize = gameManager.HandSize;
        }

        if (cardObjectsInHand.Count > 0)
        {
            ActivateCard(currentCardIndex);
            //UpdateCardVisuals();
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
        if (cardObjectsInHand.Count == 0 || index < 0 || index >= cardObjectsInHand.Count) return;

        for (int i = 0; i < cardObjectsInHand.Count; i++)
        {
            //       if (cardObjectsInHand[i] != null)
        }
    }
}
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    