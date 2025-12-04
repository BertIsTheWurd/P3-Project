using UnityEngine;

// Enum for different card types
public enum CardType
{
    Path,                    // Normal directional cards (DEFAULT)
    Start,                   // Starting card
    End,                     // End cards (3 total)
    DeadEnd,                 // Blocks all paths (Supervisor)
    BureaucraticBarrier,     // Blocks path, removable (Supervisor)
    Warrant,                 // Removes Bureaucratic Barrier (Player/Partner)
    Censor,                  // Disables a card (Supervisor)
    Uncensor,                // Removes Censor/Propaganda/Orders (Player/Partner)
    Clue,                    // Bonus turn when connected
    Peek,                    // Check one exit (Player/Partner)
    CoffeeBreak,             // Safe examination phase (Player/Partner)
    SeeMeInMyOffice,         // Skip turn (Supervisor)
    TamperWithEvidence,      // Change card direction (Supervisor)
    MandatoryPsychEval,      // Force break + tamper (Supervisor)
    Propaganda,              // Temporary barrier (Supervisor)
    OrdersFromAbove,         // Temporary barrier (Supervisor)
    Protocol                 // Removes row/column (Supervisor)
}

// Enum for card owner
public enum CardOwner
{
    Player,      // Can be played by player or partner (DEFAULT)
    Supervisor,  // Only played by supervisor
    Neutral      // Special cards like Start, End, Clue
}

[CreateAssetMenu(fileName = "NewDirectionalCard", menuName = "Cards/DirectionalCard")]
public class DirectionalCardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    public Sprite cardImage;
    public Sprite cardBackside;
    
    // New fields with defaults that maintain backward compatibility
    public CardType cardType = CardType.Path;  // Defaults to Path for existing cards
    public CardOwner cardOwner = CardOwner.Player;  // Defaults to Player for existing cards
    
    [Header("Special Flags")]
    public bool isStart = false;
    public bool isEnd = false;
    public bool isCorrect = false;  // For end cards - which is the true ending
    public bool specialCard = false; // Kept for backward compatibility
    
    [Header("Path Connections")]
    public bool connectsUp = false;
    public bool connectsDown = false;
    public bool connectsLeft = false;
    public bool connectsRight = false;
    
    [Header("Blocking Card Properties (Optional - for new card types)")]
    public bool isBlockingCard = false;  // Dead End, Bureaucratic Barrier, etc.
    public bool isRemovable = false;     // Can this blocking card be removed?
    public CardType removalCardType;     // What card type removes this?
    
    [Header("Special Card Properties (Optional - for new card types)")]
    public bool givesBonusTurn = false;  // For Clue cards
    public bool forcesSkipTurn = false;  // For See Me in My Office
    public bool allowsExamination = false; // For Coffee Break
    public bool canCheckExit = false;    // For Peek cards
    public bool disablesCard = false;    // For Censor
    public bool canChangeDirection = false; // For Tamper with Evidence
    
    [Header("Protocol Card Properties (Optional)")]
    public bool removesRowOrColumn = false;
    public int targetRow = -1;    // -1 means not set
    public int targetColumn = -1; // -1 means not set
    
    [Header("Visual Feedback (Optional)")]
    public Color cardTint = Color.white;  // Optional tint for different card types
    
    [TextArea(3, 5)]
    public string cardDescription = "";    // Tooltip/description
    
    // Helper methods
    public bool IsPathCard()
    {
        return cardType == CardType.Path || isStart || isEnd;
    }
    
    public bool IsSpecialCard()
    {
        return !IsPathCard() && cardType != CardType.DeadEnd && cardType != CardType.BureaucraticBarrier;
    }
    
    public bool CanBePlacedOnGrid()
    {
        // Cards that can be placed on the grid (not hand-only cards like Warrant, Peek, etc.)
        return IsPathCard() || 
               cardType == CardType.DeadEnd || 
               cardType == CardType.BureaucraticBarrier ||
               cardType == CardType.Censor ||
               cardType == CardType.Propaganda ||
               cardType == CardType.OrdersFromAbove ||
               cardType == CardType.Clue;
    }
    
    public bool IsTargetedAbility()
    {
        // Cards that require targeting another card
        return cardType == CardType.Warrant || 
               cardType == CardType.Uncensor ||
               cardType == CardType.Censor ||
               cardType == CardType.TamperWithEvidence ||
               cardType == CardType.Peek;
    }
    
    // Automatically set cardType based on flags (for backward compatibility)
    private void OnValidate()
    {
        // Auto-set cardType based on existing flags
        if (isStart && cardType == CardType.Path)
        {
            cardType = CardType.Start;
            cardOwner = CardOwner.Neutral;
        }
        else if (isEnd && cardType == CardType.Path)
        {
            cardType = CardType.End;
            cardOwner = CardOwner.Neutral;
        }
        
        // Auto-set blocking properties based on cardType
        switch (cardType)
        {
            case CardType.DeadEnd:
                isBlockingCard = true;
                isRemovable = false;
                cardOwner = CardOwner.Supervisor;
                break;
                
            case CardType.BureaucraticBarrier:
                isBlockingCard = true;
                isRemovable = true;
                removalCardType = CardType.Warrant;
                cardOwner = CardOwner.Supervisor;
                break;
                
            case CardType.Propaganda:
            case CardType.OrdersFromAbove:
                isBlockingCard = true;
                isRemovable = true;
                removalCardType = CardType.Uncensor;
                cardOwner = CardOwner.Supervisor;
                break;
                
            case CardType.Warrant:
            case CardType.Uncensor:
            case CardType.Peek:
            case CardType.CoffeeBreak:
                cardOwner = CardOwner.Player;
                break;
                
            case CardType.Censor:
            case CardType.SeeMeInMyOffice:
            case CardType.TamperWithEvidence:
            case CardType.MandatoryPsychEval:
            case CardType.Protocol:
                cardOwner = CardOwner.Supervisor;
                break;
                
            case CardType.Clue:
                givesBonusTurn = true;
                cardOwner = CardOwner.Neutral;
                break;
        }
    }
}