using UnityEngine;

public enum CardType { Start, Path, End, Peek, CoffeeBreak, Warrant, Bureaucracy, Censor, Uncensor, Tamper, Protocol }

[CreateAssetMenu(fileName = "Card", menuName = "CardGame/Card")]
public class CardData : ScriptableObject {
    public string cardName;
    public CardType type;
    public Sprite cardImage;

    // Add this for end cards
    public bool isCorrect;
}