using UnityEngine;

public class CardSlot
{
    public CardData card;
    public Vector2Int position;

    public CardSlot(Vector2Int pos)
    {
        position = pos;
        card = null;
    }

    public void PlaceCard(CardData newCard)
    {
        card = newCard;
    }

    public bool IsEmpty()
    {
        return card == null;
    }
}