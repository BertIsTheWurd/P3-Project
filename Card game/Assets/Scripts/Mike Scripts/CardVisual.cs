using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardVisual : MonoBehaviour, IPointerClickHandler {
    public Image cardImage; // Assign in Inspector
    public Text cardNameText; // Optional
    public Vector2Int gridPosition; // Assigned by GridManager
    private GridManager gridManager;

    public void Initialize(GridManager manager, Vector2Int pos) {
        gridManager = manager;
        gridPosition = pos;
    }

    public void SetCard(CardData card) {
        if (card != null) {
            cardImage.sprite = card.cardImage;
            cardNameText.text = card.cardName;
        } else {
            cardImage.sprite = null;
            cardNameText.text = "";
        }
    }

    public void OnPointerClick(PointerEventData eventData) {
        gridManager.OnCardSlotClicked(gridPosition);
    }
}