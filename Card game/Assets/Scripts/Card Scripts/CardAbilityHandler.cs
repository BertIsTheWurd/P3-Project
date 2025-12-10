using UnityEngine;
using System.Collections;

/// <summary>
/// Handles special card abilities like Warrant, Peek, Censor, etc.
/// </summary>
public class CardAbilityHandler : MonoBehaviour
{
    private GameManager gameManager;
    
    [Header("Ability Settings")]
    public float abilityAnimationDuration = 0.5f;
    
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("CardAbilityHandler: GameManager not found!");
        }
    }
    
    /// <summary>
    /// Execute a card's ability
    /// </summary>
    public IEnumerator ExecuteCardAbility(DirectionalCardData cardData, GameObject sourceCard = null, GameObject targetCard = null, int targetRow = -1, int targetCol = -1)
    {
        Debug.Log($"Executing ability for card: {cardData.cardName} (Type: {cardData.cardType})");
        
        switch (cardData.cardType)
        {
            case CardType.Warrant:
                yield return RemoveBureaucraticBarrier(targetCard);
                break;
                
            case CardType.Uncensor:
                yield return RemoveCensor(targetCard);
                break;
                
            case CardType.Peek:
                yield return PeekAtExit(targetRow);
                break;
                
            case CardType.CoffeeBreak:
                yield return TakeCoffeeBreak();
                break;
                
            case CardType.Censor:
                yield return CensorCard(targetCard);
                break;
                
            default:
                Debug.LogWarning($"No ability handler for card type: {cardData.cardType}");
                break;
        }
    }
    
    /// <summary>
    /// Remove a Bureaucratic Barrier with a Warrant card
    /// </summary>
    private IEnumerator RemoveBureaucraticBarrier(GameObject targetCard)
    {
        if (targetCard == null)
        {
            Debug.LogWarning("Warrant: No target card specified!");
            yield break;
        }
        
        Card card = targetCard.GetComponent<Card>();
        if (card == null || card.cardData.cardType != CardType.BureaucraticBarrier)
        {
            Debug.LogWarning("Warrant: Target is not a Bureaucratic Barrier!");
            yield break;
        }
        
        Debug.Log($"📋 Warrant: Removing Bureaucratic Barrier at {targetCard.transform.position}");
        
        // Animate removal
        yield return AnimateCardRemoval(targetCard);
        
        // Remove from grid
        gameManager.RemoveCard(targetCard);
        
        // Return to discard
        gameManager.discardPile.AddToDiscard(targetCard);
        
        Debug.Log("✅ Bureaucratic Barrier removed!");
    }
    
    /// <summary>
    /// Remove a Censor from a censored card with Uncensor
    /// </summary>
    private IEnumerator RemoveCensor(GameObject targetCard)
    {
        if (targetCard == null)
        {
            Debug.LogWarning("Uncensor: No target card specified!");
            yield break;
        }
        
        Card card = targetCard.GetComponent<Card>();
        if (card == null)
        {
            Debug.LogWarning("Uncensor: Target has no Card component!");
            yield break;
        }
        
        Debug.Log($"📰 Uncensor: Removing censor from card");
        
        // Use CensorHandler to remove censor
        CensorHandler censorHandler = FindObjectOfType<CensorHandler>();
        if (censorHandler != null)
        {
            censorHandler.RemoveCensor(targetCard);
        }
        else
        {
            // Fallback: directly remove censored state
            card.isCensored = false;
        }
        
        Debug.Log("✅ Censor removed!");
        yield return new WaitForSeconds(abilityAnimationDuration);
    }
    
    /// <summary>
    /// Peek at one of the exit cards - reveals the card face
    /// </summary>
    private IEnumerator PeekAtExit(int exitRow)
    {
        if (exitRow < 0 || exitRow >= gameManager.gridX)
        {
            Debug.LogWarning($"Peek: Invalid exit row: {exitRow}");
            yield break;
        }
        
        int exitCol = gameManager.gridZ - 1; // Rightmost column
        GameObject exitCard = gameManager.playedCards[exitRow, exitCol];
        
        if (exitCard == null)
        {
            Debug.LogWarning($"Peek: No exit card at row {exitRow}");
            yield break;
        }
        
        Card card = exitCard.GetComponent<Card>();
        if (card == null || !card.cardData.isEnd)
        {
            Debug.LogWarning($"Peek: Card at row {exitRow} is not an exit card!");
            yield break;
        }
        
        Debug.Log($"🔍 Peek: Revealing exit at row {exitRow}...");
        
        // Animate the peek (pulse the card)
        yield return AnimatePeek(exitCard);
        
        // Permanently reveal the card
        gameManager.RevealEndCard(exitRow);
        
        // Show result in console
        bool isCorrect = card.cardData.isCorrect;
        string result = isCorrect ? "✅ CORRECT EXIT" : "❌ WRONG EXIT";
        Debug.Log($"🔍 Peek Result: Row {exitRow} is {result}");
        
        // You could add UI feedback here
    }
    
    /// <summary>
    /// Take a coffee break - safe examination phase
    /// </summary>
    private IEnumerator TakeCoffeeBreak()
    {
        Debug.Log("☕ Coffee Break: Player taking a break...");
        
        // Pause game
        gameManager.enabled = false;
        
        // Show UI message or allow room examination
        // This would connect to your examination system
        
        yield return new WaitForSeconds(2f); // Placeholder
        
        // Resume game
        gameManager.enabled = true;
        
        Debug.Log("☕ Coffee Break ended. Board is safe!");
    }
    
    /// <summary>
    /// Censor a card (disable it with overlay)
    /// Note: This is primarily called from SupervisorAI, which handles placement
    /// </summary>
    private IEnumerator CensorCard(GameObject targetCard)
    {
        if (targetCard == null)
        {
            Debug.LogWarning("Censor: No target card specified!");
            yield break;
        }
        
        Card card = targetCard.GetComponent<Card>();
        if (card == null)
        {
            Debug.LogWarning("Censor: Target has no Card component!");
            yield break;
        }
        
        Debug.Log($"🚫 Censor: Disabling card {card.cardData.cardName}");
        
        // Mark as censored (physical placement handled by SupervisorAI)
        card.isCensored = true;
        
        Debug.Log("✅ Card censored!");
        yield return new WaitForSeconds(abilityAnimationDuration);
    }
    
    // Animation helpers
    private IEnumerator AnimateCardRemoval(GameObject card)
    {
        float elapsed = 0f;
        Vector3 originalScale = card.transform.localScale;
        SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        
        while (elapsed < abilityAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / abilityAnimationDuration;
            
            card.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            if (sr != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }
            
            yield return null;
        }
    }
    
    private IEnumerator AnimatePeek(GameObject exitCard)
    {
        // Pulse animation to draw attention
        Vector3 originalScale = exitCard.transform.localScale;
        float elapsed = 0f;
        float duration = abilityAnimationDuration;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed / duration * 2, 1);
            exitCard.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);
            yield return null;
        }
        
        exitCard.transform.localScale = originalScale;
    }
    
    private IEnumerator AnimateCensor(GameObject card)
    {
        SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        
        Color originalColor = sr.color;
        float elapsed = 0f;
        
        while (elapsed < abilityAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / abilityAnimationDuration;
            sr.color = Color.Lerp(originalColor, new Color(0.3f, 0.3f, 0.3f, 0.7f), t);
            yield return null;
        }
    }
}