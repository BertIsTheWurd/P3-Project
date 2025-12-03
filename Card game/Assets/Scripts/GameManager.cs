using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridX = 5; // rows (height) - set to 5 in inspector
    public int gridZ = 7; // columns (width) - set to 7 in inspector
    public GameObject gridSlotPrefab;
    public Sprite gridSlotSprite;
    public Transform gridCenter; // Optional center point

    [Header("Card Data")]
    public DirectionalCardData startCardData;
    public DirectionalCardData correctEndCardData;
    public DirectionalCardData wrongEndCardData;

    [Header("References")]
    public CardPool cardPool;
    public DrawPile drawPile;
    public DiscardPile discardPile;
    public CardsInHandController handController;
    public GameObject[,] playedCards;
    public Vector3[,] cardSlots;
    private GameObject[,] gridSlotObjects;

    [Header("Player Settings")]
    public int HandSize = 6;

    [Header("UDP Listener")]
    private UdpClient udpClient;
    public int port = 5005;
    public bool lookingAway = false;

    private int correctEndRow = -1;
    
    // Track last played card and all card positions
    public GameObject lastPlayedCard;
    private Dictionary<GameObject, Vector2Int> cardPositions = new Dictionary<GameObject, Vector2Int>();

    private void Start()
    {
        playedCards = new GameObject[gridX, gridZ];
        cardSlots = new Vector3[gridX, gridZ];
        gridSlotObjects = new GameObject[gridX, gridZ];

        CreateGridSlots();
        PlaceStartAndEndCards();
        StartCoroutine(InitializeGameAfterCardPool());
    }

    private System.Collections.IEnumerator InitializeGameAfterCardPool()
    {
        yield return null;
        drawPile.Initialize(cardPool.cards);
        DrawUntilFullHand();

        udpClient = new UdpClient(port);
        udpClient.BeginReceive(ReceiveCallback, null);
        Debug.Log("UDP Listener started on port " + port);
    }

    private void CreateGridSlots()
    {
        float slotWidth = gridSlotSprite.bounds.size.x * gridSlotPrefab.transform.localScale.x;
        float slotHeight = gridSlotSprite.bounds.size.y * gridSlotPrefab.transform.localScale.y;

        // gridX=5 (rows), gridZ=7 (columns)
        // We want 7 wide (left-right), 5 tall (top-bottom)
        float offsetX = (gridZ - 1) * slotWidth / 2f;   // gridZ=7 for width
        float offsetZ = (gridX - 1) * slotHeight / 2f;  // gridX=5 for height

        Vector3 center = gridCenter != null ? gridCenter.position : Vector3.zero;

        for (int row = 0; row < gridX; row++) // 5 rows
        {
            for (int col = 0; col < gridZ; col++) // 7 columns
            {
                Vector3 pos = new Vector3(
                    center.x + (col * slotWidth) - offsetX,      // col controls X (left-right)
                    center.y,
                    center.z + (row * slotHeight) - offsetZ      // row controls Z (top-bottom)
                );

                var slot = Instantiate(gridSlotPrefab, pos, Quaternion.Euler(90, 0, 0));
                slot.tag = "GridSlot";

                if (slot.GetComponent<GridSlot>() == null)
                    slot.AddComponent<GridSlot>();

                var gridSlot = slot.GetComponent<GridSlot>();
                if (gridSlot != null && gridSlotSprite != null)
                    gridSlot.emptySlotSprite = gridSlotSprite;

                gridSlotObjects[row, col] = slot;
                cardSlots[row, col] = pos;
            }
        }
    }

    private void PlaceStartAndEndCards()
    {
        // Array is [row, col] where row is 0-4 (gridX=5) and col is 0-6 (gridZ=7)
        // Start: middle row (2 of 5), leftmost column (0 of 7)
        // End: top/middle/bottom rows (0,2,4 of 5), rightmost column (6 of 7)
        
        int startRow = gridX / 2;  // = 2 (middle of 5 rows)
        int startCol = 0;           // = 0 (leftmost of 7 columns)
        
        var startCardObj = cardPool.CreateSpecialCard(startCardData);
        startCardObj.transform.position = cardSlots[startRow, startCol];
        startCardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        startCardObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        startCardObj.SetActive(true);
        playedCards[startRow, startCol] = startCardObj;
        cardPositions[startCardObj] = new Vector2Int(startCol, startRow);
        gridSlotObjects[startRow, startCol]?.GetComponent<GridSlot>()?.ShowAsOccupied();
        
        Debug.Log($"Start card placed at row {startRow}, col {startCol}");

        int[] endRows = { 0, 2, 4 };  // Top, middle, bottom of 5 rows
        int endColumn = gridZ - 1;     // = 6 (rightmost of 7 columns)
        correctEndRow = endRows[UnityEngine.Random.Range(0, endRows.Length)];

        foreach (int row in endRows)
        {
            DirectionalCardData endData = (row == correctEndRow) ? correctEndCardData : wrongEndCardData;
            var endCardObj = cardPool.CreateSpecialCard(endData);
            endCardObj.transform.position = cardSlots[row, endColumn];
            endCardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            endCardObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            endCardObj.SetActive(true);
            playedCards[row, endColumn] = endCardObj;
            cardPositions[endCardObj] = new Vector2Int(endColumn, row);
            gridSlotObjects[row, endColumn]?.GetComponent<GridSlot>()?.ShowAsOccupied();
            
            Debug.Log($"End card placed at row {row}, col {endColumn}");
        }

        Debug.Log($"Correct end is at row {correctEndRow}");
    }

    public void DrawUntilFullHand()
    {
        while (handController.HandCount < HandSize)
        {
            var card = drawPile.DrawCard();
            if (card == null)
            {
                Debug.Log("No more cards to draw!");
                break;
            }
            handController.AddCardToHand(card);
        }
    }

    public void DiscardCard(GameObject card)
    {
        discardPile.AddToDiscard(card);
    }

    public bool CanPlaceCard(int col, int row, DirectionalCardData newCard)
    {
        // col is 0-6 (gridZ), row is 0-4 (gridX)
        Debug.Log($"Checking placement at row {row}, col {col}");
        
        // Check bounds
        if (row < 0 || row >= gridX || col < 0 || col >= gridZ)
        {
            Debug.Log("Out of bounds");
            return false;
        }
        
        // Check if slot is occupied
        if (playedCards[row, col] != null)
        {
            Debug.Log("Slot already occupied");
            return false;
        }
        
        // Start card: middle row (gridX/2), first column (0)
        // End cards: last column (gridZ-1), rows 0, 2, 4
        if ((row == gridX / 2 && col == 0) ||
            (col == gridZ - 1 && (row == 0 || row == 2 || row == 4)))
        {
            Debug.Log("Cannot place on start/end positions");
            return false;
        }

        bool hasValidConnection = false;
        bool hasInvalidConnection = false;

        // Check up (row - 1)
        if (row > 0 && playedCards[row - 1, col] != null)
        {
            var neighbor = playedCards[row - 1, col].GetComponent<Card>();
            // If neighbor has a connection pointing down, new card MUST connect up
            if (neighbor.ConnectsDown)
            {
                if (newCard.connectsUp)
                {
                    hasValidConnection = true;
                    Debug.Log("Valid connection UP");
                }
                else
                {
                    Debug.Log("Invalid: neighbor connects down but new card doesn't connect up");
                    hasInvalidConnection = true;
                }
            }
            // If new card connects up, neighbor MUST connect down
            else if (newCard.connectsUp)
            {
                Debug.Log("Invalid: new card connects up but neighbor doesn't connect down");
                hasInvalidConnection = true;
            }
        }
        
        // Check down (row + 1)
        if (row < gridX - 1 && playedCards[row + 1, col] != null)
        {
            var neighbor = playedCards[row + 1, col].GetComponent<Card>();
            if (neighbor.ConnectsUp)
            {
                if (newCard.connectsDown)
                {
                    hasValidConnection = true;
                    Debug.Log("Valid connection DOWN");
                }
                else
                {
                    Debug.Log("Invalid: neighbor connects up but new card doesn't connect down");
                    hasInvalidConnection = true;
                }
            }
            else if (newCard.connectsDown)
            {
                Debug.Log("Invalid: new card connects down but neighbor doesn't connect up");
                hasInvalidConnection = true;
            }
        }
        
        // Check left (col - 1)
        if (col > 0 && playedCards[row, col - 1] != null)
        {
            var neighbor = playedCards[row, col - 1].GetComponent<Card>();
            if (neighbor.ConnectsRight)
            {
                if (newCard.connectsLeft)
                {
                    hasValidConnection = true;
                    Debug.Log("Valid connection LEFT");
                }
                else
                {
                    Debug.Log("Invalid: neighbor connects right but new card doesn't connect left");
                    hasInvalidConnection = true;
                }
            }
            else if (newCard.connectsLeft)
            {
                Debug.Log("Invalid: new card connects left but neighbor doesn't connect right");
                hasInvalidConnection = true;
            }
        }
        
        // Check right (col + 1)
        if (col < gridZ - 1 && playedCards[row, col + 1] != null)
        {
            var neighbor = playedCards[row, col + 1].GetComponent<Card>();
            if (neighbor.ConnectsLeft)
            {
                if (newCard.connectsRight)
                {
                    hasValidConnection = true;
                    Debug.Log("Valid connection RIGHT");
                }
                else
                {
                    Debug.Log("Invalid: neighbor connects left but new card doesn't connect right");
                    hasInvalidConnection = true;
                }
            }
            else if (newCard.connectsRight)
            {
                Debug.Log("Invalid: new card connects right but neighbor doesn't connect left");
                hasInvalidConnection = true;
            }
        }

        // Card is valid if it has at least one valid connection and no invalid connections
        if (hasInvalidConnection)
        {
            Debug.Log("Card has invalid connections with neighbors");
            return false;
        }
        
        if (!hasValidConnection)
        {
            Debug.Log("No valid connections found - card must connect to at least one adjacent card");
            return false;
        }
        
        return true;
    }

    public void PlayCard(GameObject cardObj, int col, int row)
    {
        var card = cardObj.GetComponent<Card>();
        Debug.Log($"Attempting to play card at row {row}, col {col}");
        
        if (!CanPlaceCard(col, row, card.cardData))
        {
            Debug.Log("Invalid placement: path does not connect or no adjacent cards.");
            return;
        }

        handController.RemoveCardFromHand(cardObj);
        cardObj.transform.SetParent(null);
        cardObj.transform.position = cardSlots[row, col];
        cardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        // Keep the scale consistent with other cards on the grid (0.25, 0.25, 0.25)
        cardObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        cardObj.tag = "PlayedCard";
        cardObj.SetActive(true);
        playedCards[row, col] = cardObj;
        
        // Store position and track as last played card
        cardPositions[cardObj] = new Vector2Int(col, row);
        lastPlayedCard = cardObj;
        
        gridSlotObjects[row, col]?.GetComponent<GridSlot>()?.ShowAsOccupied();

        Debug.Log($"Card successfully played at row {row}, col {col}");
        
        DrawUntilFullHand();

        var pathResult = ValidatePath();
        if (pathResult.isComplete)
        {
            if (pathResult.isCorrect)
            {
                Debug.Log("🎉 Path complete! You found the CORRECT exit!");
                OnVictory();
            }
            else
            {
                Debug.Log("❌ Path complete, but this is the WRONG exit!");
                OnWrongExit();
            }
        }
    }
    
    // Helper method to get a card's position
    public Vector2Int? GetCardPosition(GameObject card)
    {
        if (cardPositions.ContainsKey(card))
        {
            return cardPositions[card];
        }
        return null;
    }
    
    // Helper method to remove a card from the grid
    public void RemoveCard(GameObject card)
    {
        if (cardPositions.ContainsKey(card))
        {
            Vector2Int pos = cardPositions[card];
            playedCards[pos.y, pos.x] = null;
            cardPositions.Remove(card);
            gridSlotObjects[pos.y, pos.x]?.GetComponent<GridSlot>()?.ShowAsEmpty();
            
            if (lastPlayedCard == card)
            {
                lastPlayedCard = null;
            }
            
            Debug.Log($"Removed card from row {pos.y}, col {pos.x}");
        }
    }

    private (bool isComplete, bool isCorrect) ValidatePath()
    {
        int startRow = gridX / 2;  // middle row
        int startCol = 0;           // first column
        if (playedCards[startRow, startCol] == null) return (false, false);

        HashSet<(int row, int col)> visited = new HashSet<(int, int)>();
        Queue<(int row, int col)> queue = new Queue<(int, int)>();
        queue.Enqueue((startRow, startCol));

        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            if (visited.Contains((row, col))) continue;
            visited.Add((row, col));

            var currentCard = playedCards[row, col].GetComponent<Card>();
            if (col == gridZ - 1 && currentCard.cardData.isEnd)  // last column
            {
                bool isCorrect = (row == correctEndRow);
                return (true, isCorrect);
            }

            if (currentCard.ConnectsUp && row > 0 && playedCards[row - 1, col] != null)
            {
                var neighbor = playedCards[row - 1, col].GetComponent<Card>();
                if (neighbor.ConnectsDown) queue.Enqueue((row - 1, col));
            }
            if (currentCard.ConnectsDown && row < gridX - 1 && playedCards[row + 1, col] != null)
            {
                var neighbor = playedCards[row + 1, col].GetComponent<Card>();
                if (neighbor.ConnectsUp) queue.Enqueue((row + 1, col));
            }
            if (currentCard.ConnectsLeft && col > 0 && playedCards[row, col - 1] != null)
            {
                var neighbor = playedCards[row, col - 1].GetComponent<Card>();
                if (neighbor.ConnectsRight) queue.Enqueue((row, col - 1));
            }
            if (currentCard.ConnectsRight && col < gridZ - 1 && playedCards[row, col + 1] != null)
            {
                var neighbor = playedCards[row, col + 1].GetComponent<Card>();
                if (neighbor.ConnectsLeft) queue.Enqueue((row, col + 1));
            }
        }

        return (false, false);
    }

    private void OnVictory() => enabled = false;
    private void OnWrongExit() { }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            byte[] bytes = udpClient.EndReceive(ar, ref ip);
            string message = Encoding.UTF8.GetString(bytes);
            if (message == "LOOKING_AWAY")
            {
                lookingAway = true;
                Debug.Log("👁️ lookingAway = TRUE");
            }
            else if (message == "LOOKING")
            {
                lookingAway = false;
                Debug.Log("👁️ lookingAway = FALSE");
            }
            udpClient.BeginReceive(ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Error: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}