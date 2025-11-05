using UnityEngine;

public class GridManager : MonoBehaviour {
    public int columns = 7;
    public int rows = 5;
    public CardSlot[,] grid;

    public CardData startCard;
    public CardData[] weaponCards;
    public CardData[] motiveCards;
    public CardData[] culpritCards;

    public GameObject cardPrefab;
    public Transform gridParent;
    public float cellWidth = 150f; // Adjust for UI
    public float cellHeight = 200f;

    void Start() {
        InitializeGrid();
        SetupEndCards(1); // Example: Round 1
        RenderGrid();
    }

    void InitializeGrid() {
        grid = new CardSlot[columns, rows];
        for (int x = 0; x < columns; x++) {
            for (int y = 0; y < rows; y++) {
                grid[x, y] = new CardSlot(new Vector2Int(x, y));
            }
        }

        // Place start card at column 0, row 2
        grid[0, 2].PlaceCard(startCard);
    }

    void SetupEndCards(int round) {
        CardData[] cardsToUse = round == 1 ? weaponCards :
                                 round == 2 ? motiveCards : culpritCards;

        Shuffle(cardsToUse);

        // Place end cards in column 6, rows 0, 2, 4
        grid[6, 0].PlaceCard(cardsToUse[0]);
        grid[6, 2].PlaceCard(cardsToUse[1]);
        grid[6, 4].PlaceCard(cardsToUse[2]);

        // Mark one as correct
        cardsToUse[Random.Range(0, 3)].isCorrect = true;
    }

    void Shuffle(CardData[] array) {
        for (int i = 0; i < array.Length; i++) {
            int rand = Random.Range(i, array.Length);
            CardData temp = array[i];
            array[i] = array[rand];
            array[rand] = temp;
        }
    }

    void RenderGrid() {
        for (int x = 0; x < columns; x++) {
            for (int y = 0; y < rows; y++) {
                Vector3 position = new Vector3(x * cellWidth, y * cellHeight, 0);
                GameObject cardObj = Instantiate(cardPrefab, position, Quaternion.identity, gridParent);

                CardVisual visual = cardObj.GetComponent<CardVisual>();
                visual.Initialize(this, new Vector2Int(x, y));
                visual.SetCard(grid[x, y].card);
            }
        }
    }

    public void OnCardSlotClicked(Vector2Int pos) {
        CardSlot slot = grid[pos.x, pos.y];

        if (!slot.IsEmpty()) {
            Debug.Log("Slot already occupied!");
            return;
        }

        CardData cardToPlace = GetCardFromHand();
        if (cardToPlace == null) {
            Debug.Log("No card selected!");
            return;
        }

        slot.PlaceCard(cardToPlace);
        UpdateCardVisual(pos, cardToPlace);
    }

    void UpdateCardVisual(Vector2Int pos, CardData card) {
        foreach (Transform child in gridParent) {
            CardVisual visual = child.GetComponent<CardVisual>();
            if (visual != null && visual.gridPosition == pos) {
                visual.SetCard(card);
                break;
            }
        }
    }

    CardData GetCardFromHand() {
        // Temporary: return a random weapon card for testing
        return weaponCards[Random.Range(0, weaponCards.Length)];
    }
}