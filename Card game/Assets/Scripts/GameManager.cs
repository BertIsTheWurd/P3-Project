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
    public GameEndUI gameEndUI;
    public GameObject[,] playedCards;
    public Vector3[,] cardSlots;
    private GameObject[,] gridSlotObjects;
    
    // Performance: Cache Card components to avoid expensive GetComponent calls in pathfinding
    private Dictionary<GameObject, Card> cardCache = new Dictionary<GameObject, Card>();

    [Header("Player Settings")]
    public int HandSize = 6;

    [Header("UDP Listener")]
    private UdpClient udpClient;
    public int port = 5005;
    public bool lookingAway = false;
    private bool previousLookingAwayState = false; // Track state changes

    [Header("Looking Away Sabotage")]
    public float sabotageCheckInterval = 0.5f; // How often to check if player is looking away
    public float sabotageGracePeriod = 2f; // How long player can look away before sabotage
    public float sabotageAnimationDuration = 1f; // How long the removal animation takes
    private float lookingAwayTimer = 0f; // How long player has been looking away
    private bool isSabotageActive = false; // Prevent multiple simultaneous sabotages

    private int correctEndRow = -1;
    
    // Track last played card and all card positions
    public GameObject lastPlayedCard;
    public GameObject lastPlayerCard; // Track last PLAYER card specifically for sabotage
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
    
    private void Update()
    {
        // Continuously monitor looking away state
        if (lookingAway && lastPlayerCard != null && !isSabotageActive)
        {
            // Player is looking away - increment timer
            lookingAwayTimer += Time.deltaTime;
            
            // Check if grace period has expired
            if (lookingAwayTimer >= sabotageGracePeriod)
            {
                Debug.Log($"Sabotage triggered - player looked away for {lookingAwayTimer:F1}s");
                StartCoroutine(ExecuteSabotage());
            }
        }
        else
        {
            // Player is looking at screen or no player card to sabotage - reset timer
            if (lookingAwayTimer > 0)
            {
                Debug.Log($"Sabotage timer reset - player returned after {lookingAwayTimer:F1}s");
            }
            lookingAwayTimer = 0f;
        }
    }

    private System.Collections.IEnumerator InitializeGameAfterCardPool()
    {
        yield return null;
        drawPile.Initialize(cardPool.cards);
        DrawUntilFullHand();

        // Play intro voice line
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.playIntroLine?.Invoke();
        }

        // Initialize UDP with error handling
        try
        {
            // Close any existing connection first
            CleanupUDP();
            
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            udpClient.BeginReceive(ReceiveCallback, null);
            Debug.Log("UDP Listener started on port " + port);
        }
        catch (SocketException e)
        {
            Debug.LogError($"Failed to start UDP Listener on port {port}: {e.Message}");
            Debug.LogError("Try restarting Unity or changing the port number.");
        }
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

        for (int row = 0; row < gridX; row++) // 5 rows (0 = top, 4 = bottom)
        {
            for (int col = 0; col < gridZ; col++) // 7 columns (0 = left, 6 = right)
            {
                // INVERTED: row 0 should be at HIGHEST Z (furthest from camera)
                // So we use (gridX - 1 - row) instead of row for Z calculation
                Vector3 pos = new Vector3(
                    center.x + (col * slotWidth) - offsetX,           // col controls X (left-right)
                    center.y,
                    center.z + ((gridX - 1 - row) * slotHeight) - offsetZ  // INVERTED row for Z
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
        
        // Cache the start card for pathfinding
        Card startCardComponent = startCardObj.GetComponent<Card>();
        if (startCardComponent != null)
        {
            cardCache[startCardObj] = startCardComponent;
        }
        
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
            
            // Set end cards to be face-down initially
            Card cardComponent = endCardObj.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.SetFaceDown(true);
                // Cache the end card for pathfinding
                cardCache[endCardObj] = cardComponent;
            }
            
            playedCards[row, endColumn] = endCardObj;
            cardPositions[endCardObj] = new Vector2Int(endColumn, row);
            gridSlotObjects[row, endColumn]?.GetComponent<GridSlot>()?.ShowAsOccupied();
            
            Debug.Log($"End card placed face-down at row {row}, col {endColumn}");
        }

        Debug.Log($"Correct end is at row {correctEndRow} (hidden)");
    }
    
    /// <summary>
    /// Reveal an end card (used by Peek ability)
    /// </summary>
    public void RevealEndCard(int row)
    {
        int endColumn = gridZ - 1;
        GameObject endCard = playedCards[row, endColumn];
        
        if (endCard != null)
        {
            Card cardComponent = endCard.GetComponent<Card>();
            if (cardComponent != null && cardComponent.cardData.isEnd)
            {
                cardComponent.SetFaceDown(false);
                Debug.Log($"Revealed end card at row {row}");
            }
        }
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

    public bool CanPlaceCard(int col, int row, GameObject cardObj)
    {
        // col is 0-6 (gridZ), row is 0-4 (gridX)
        Debug.Log($"Checking placement at row {row}, col {col}");
        
        var newCard = cardObj.GetComponent<Card>();
        if (newCard == null)
        {
            Debug.LogError("Card component not found on GameObject!");
            return false;
        }
        
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
        int adjacentCardCount = 0;

        // Check up (row - 1)
        if (row > 0 && playedCards[row - 1, col] != null)
        {
            adjacentCardCount++;
            var neighbor = playedCards[row - 1, col].GetComponent<Card>();
            
            // Both cards must agree on the connection - use Card properties that account for rotation
            if (newCard.ConnectsUp && neighbor.ConnectsDown)
            {
                hasValidConnection = true;
                Debug.Log("Valid connection UP");
            }
            else if (newCard.ConnectsUp && !neighbor.ConnectsDown)
            {
                Debug.Log("Mismatch UP: new card connects up but neighbor doesn't connect down");
            }
            else if (!newCard.ConnectsUp && neighbor.ConnectsDown)
            {
                Debug.Log("Mismatch UP: neighbor connects down but new card doesn't connect up");
            }
            // else: neither connects in this direction - that's fine
        }
        
        // Check down (row + 1)
        if (row < gridX - 1 && playedCards[row + 1, col] != null)
        {
            adjacentCardCount++;
            var neighbor = playedCards[row + 1, col].GetComponent<Card>();
            
            if (newCard.ConnectsDown && neighbor.ConnectsUp)
            {
                hasValidConnection = true;
                Debug.Log("Valid connection DOWN");
            }
            else if (newCard.ConnectsDown && !neighbor.ConnectsUp)
            {
                Debug.Log("Mismatch DOWN: new card connects down but neighbor doesn't connect up");
            }
            else if (!newCard.ConnectsDown && neighbor.ConnectsUp)
            {
                Debug.Log("Mismatch DOWN: neighbor connects up but new card doesn't connect down");
            }
        }
        
        // Check left (col - 1)
        if (col > 0 && playedCards[row, col - 1] != null)
        {
            adjacentCardCount++;
            var neighbor = playedCards[row, col - 1].GetComponent<Card>();
            
            if (newCard.ConnectsLeft && neighbor.ConnectsRight)
            {
                hasValidConnection = true;
                Debug.Log("Valid connection LEFT");
            }
            else if (newCard.ConnectsLeft && !neighbor.ConnectsRight)
            {
                Debug.Log("Mismatch LEFT: new card connects left but neighbor doesn't connect right");
            }
            else if (!newCard.ConnectsLeft && neighbor.ConnectsRight)
            {
                Debug.Log("Mismatch LEFT: neighbor connects right but new card doesn't connect left");
            }
        }
        
        // Check right (col + 1)
        if (col < gridZ - 1 && playedCards[row, col + 1] != null)
        {
            adjacentCardCount++;
            var neighbor = playedCards[row, col + 1].GetComponent<Card>();
            
            if (newCard.ConnectsRight && neighbor.ConnectsLeft)
            {
                hasValidConnection = true;
                Debug.Log("Valid connection RIGHT");
            }
            else if (newCard.ConnectsRight && !neighbor.ConnectsLeft)
            {
                Debug.Log("Mismatch RIGHT: new card connects right but neighbor doesn't connect left");
            }
            else if (!newCard.ConnectsRight && neighbor.ConnectsLeft)
            {
                Debug.Log("Mismatch RIGHT: neighbor connects left but new card doesn't connect right");
            }
        }

        // Must have at least one adjacent card
        if (adjacentCardCount == 0)
        {
            Debug.Log("No adjacent cards - card must be placed next to existing cards");
            return false;
        }
        
        // Must have at least one valid connection
        if (!hasValidConnection)
        {
            Debug.Log("No valid connections found - card must connect to at least one adjacent card");
            return false;
        }
        
        // CRITICAL: Check that at least one connecting neighbor is reachable from the START card
        // This prevents placing cards next to end cards without a path from start
        if (!HasConnectionReachableFromStart(col, row, newCard))
        {
            Debug.Log("Invalid placement: connecting neighbor is not reachable from start card");
            return false;
        }
        
        Debug.Log("Card placement is valid!");
        return true;
    }
    
    /// <summary>
    /// Check if any of the neighbors that would connect to the new card are reachable from start.
    /// This prevents placing cards that connect to end cards without a valid path from start.
    /// </summary>
    private bool HasConnectionReachableFromStart(int col, int row, Card newCard)
    {
        int startRow = gridX / 2;
        int startCol = 0;
        
        // Get all neighbors that would form a valid connection with the new card
        List<(int neighborRow, int neighborCol)> connectingNeighbors = new List<(int, int)>();
        
        // Check up
        if (row > 0 && playedCards[row - 1, col] != null)
        {
            var neighbor = GetCachedCard(playedCards[row - 1, col]);
            if (neighbor != null && newCard.ConnectsUp && neighbor.ConnectsDown)
            {
                connectingNeighbors.Add((row - 1, col));
            }
        }
        
        // Check down
        if (row < gridX - 1 && playedCards[row + 1, col] != null)
        {
            var neighbor = GetCachedCard(playedCards[row + 1, col]);
            if (neighbor != null && newCard.ConnectsDown && neighbor.ConnectsUp)
            {
                connectingNeighbors.Add((row + 1, col));
            }
        }
        
        // Check left
        if (col > 0 && playedCards[row, col - 1] != null)
        {
            var neighbor = GetCachedCard(playedCards[row, col - 1]);
            if (neighbor != null && newCard.ConnectsLeft && neighbor.ConnectsRight)
            {
                connectingNeighbors.Add((row, col - 1));
            }
        }
        
        // Check right
        if (col < gridZ - 1 && playedCards[row, col + 1] != null)
        {
            var neighbor = GetCachedCard(playedCards[row, col + 1]);
            if (neighbor != null && newCard.ConnectsRight && neighbor.ConnectsLeft)
            {
                connectingNeighbors.Add((row, col + 1));
            }
        }
        
        // For each connecting neighbor, check if it's reachable from start via valid path connections
        foreach (var (neighborRow, neighborCol) in connectingNeighbors)
        {
            if (IsCardReachableFromStart(neighborRow, neighborCol))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a specific card position is reachable from the start card via valid path connections.
    /// Uses BFS following actual card connections (not flood-fill through empty spaces).
    /// </summary>
    private bool IsCardReachableFromStart(int targetRow, int targetCol)
    {
        int startRow = gridX / 2;
        int startCol = 0;
        
        // If target is the start card itself, it's reachable
        if (targetRow == startRow && targetCol == startCol)
        {
            return true;
        }
        
        // BFS from start, following actual card connections
        HashSet<(int row, int col)> visited = new HashSet<(int, int)>();
        Queue<(int row, int col)> queue = new Queue<(int, int)>();
        queue.Enqueue((startRow, startCol));
        
        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            if (visited.Contains((row, col))) continue;
            visited.Add((row, col));
            
            // Found the target!
            if (row == targetRow && col == targetCol)
            {
                return true;
            }
            
            GameObject cardObj = playedCards[row, col];
            if (cardObj == null) continue;
            
            Card currentCard = GetCachedCard(cardObj);
            if (currentCard == null) continue;
            
            // Skip blocking cards (they break the path) - but not start/end cards
            if (!currentCard.cardData.isStart && !currentCard.cardData.isEnd)
            {
                if (currentCard.cardData.cardType == CardType.DeadEnd ||
                    currentCard.cardData.cardType == CardType.BureaucraticBarrier)
                {
                    continue;
                }
                
                // Skip censored cards
                if (currentCard.isCensored)
                {
                    continue;
                }
            }
            
            // Follow connections to neighbors
            if (currentCard.ConnectsUp && row > 0 && playedCards[row - 1, col] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row - 1, col]);
                if (neighbor != null && neighbor.ConnectsDown)
                {
                    queue.Enqueue((row - 1, col));
                }
            }
            
            if (currentCard.ConnectsDown && row < gridX - 1 && playedCards[row + 1, col] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row + 1, col]);
                if (neighbor != null && neighbor.ConnectsUp)
                {
                    queue.Enqueue((row + 1, col));
                }
            }
            
            if (currentCard.ConnectsLeft && col > 0 && playedCards[row, col - 1] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row, col - 1]);
                if (neighbor != null && neighbor.ConnectsRight)
                {
                    queue.Enqueue((row, col - 1));
                }
            }
            
            if (currentCard.ConnectsRight && col < gridZ - 1 && playedCards[row, col + 1] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row, col + 1]);
                if (neighbor != null && neighbor.ConnectsLeft)
                {
                    queue.Enqueue((row, col + 1));
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Helper to get cached Card component or fetch and cache it
    /// </summary>
    private Card GetCachedCard(GameObject cardObj)
    {
        if (cardObj == null) return null;
        
        if (cardCache.TryGetValue(cardObj, out Card cached))
        {
            return cached;
        }
        
        Card card = cardObj.GetComponent<Card>();
        if (card != null)
        {
            cardCache[cardObj] = card;
        }
        return card;
    }

    public void PlayCard(GameObject cardObj, int col, int row)
    {
        var card = cardObj.GetComponent<Card>();
        Debug.Log($"Attempting to play card at row {row}, col {col}");
        
        // Pass the GameObject so rotation is considered
        if (!CanPlaceCard(col, row, cardObj))
        {
            Debug.Log("Invalid placement: path does not connect or no adjacent cards.");
            return;
        }

        handController.RemoveCardFromHand(cardObj);
        cardObj.transform.SetParent(null);
        cardObj.transform.position = cardSlots[row, col];
        
        // Use the Card's stored rotation state (0 or 180) for clean placement
        int cardRotation = card.GetRotation();
        cardObj.transform.rotation = Quaternion.Euler(90, 0, cardRotation);
        
        // Keep the scale consistent with other cards on the grid (0.25, 0.25, 0.25)
        cardObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        
        // Reset color to white (remove any selection tint)
        var spriteRenderer = cardObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        cardObj.tag = "PlayedCard";
        cardObj.SetActive(true);
        playedCards[row, col] = cardObj;
        
        // Store position and track as last played card
        cardPositions[cardObj] = new Vector2Int(col, row);
        lastPlayedCard = cardObj;
        
        // Cache Card component for performance (avoids GetComponent in pathfinding)
        Card cardComponent = cardObj.GetComponent<Card>();
        if (cardComponent != null)
        {
            cardCache[cardObj] = cardComponent;
        }
        
        // Track last PLAYER card specifically for sabotage
        if (cardComponent != null && cardComponent.cardData != null && cardComponent.cardData.cardOwner == CardOwner.Player)
        {
            lastPlayerCard = cardObj;
            Debug.Log($"Player card tracked at ({col}, {row}) for sabotage monitoring");
            // Note: Sabotage is now handled continuously in Update(), not on card placement
        }
        else
        {
            Debug.Log($"Supervisor card placed at ({col}, {row})");
        }
        
        gridSlotObjects[row, col]?.GetComponent<GridSlot>()?.ShowAsOccupied();

        Debug.Log($"Card successfully placed at ({col}, {row})");
        
        DrawUntilFullHand();

        var pathResult = ValidatePath();
        if (pathResult.isComplete)
        {
            if (pathResult.isCorrect)
            {
                Debug.Log("Path complete - CORRECT exit found!");
                OnVictory();
            }
            else
            {
                Debug.Log("Path complete - WRONG exit reached!");
                OnWrongExit();
            }
        }
        else
        {
            // Check if player or Supervisor has blocked all paths
            CheckForGameOver();
            
            // Player turn ended - notify Supervisor
            SupervisorAI supervisor = FindObjectOfType<SupervisorAI>();
            if (supervisor != null)
            {
                supervisor.OnPlayerTurnEnd();
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
            cardCache.Remove(card); // Clear from performance cache
            gridSlotObjects[pos.y, pos.x]?.GetComponent<GridSlot>()?.ShowAsEmpty();
            
            if (lastPlayedCard == card)
            {
                lastPlayedCard = null;
            }
            
            if (lastPlayerCard == card)
            {
                lastPlayerCard = null;
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
        
        bool crossedWrongExit = false;
        bool reachedCorrectExit = false;
        bool reachedWrongExit = false;

        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            if (visited.Contains((row, col))) continue;
            visited.Add((row, col));

            GameObject cardObj = playedCards[row, col];
            if (!cardCache.TryGetValue(cardObj, out Card currentCard)) continue;
            
            // Skip blocking cards - they break the path (except for start/end cards)
            if (!currentCard.cardData.isStart && !currentCard.cardData.isEnd)
            {
                // Check if card is a blocking type
                if (currentCard.cardData.cardType == CardType.DeadEnd ||
                    currentCard.cardData.cardType == CardType.BureaucraticBarrier)
                {
                    Debug.Log($"Path blocked by {currentCard.cardData.cardType} at row {row}, col {col}");
                    continue; // Don't traverse through blocking cards
                }
                
                // Check if card is censored (blocked until uncensored)
                if (currentCard.isCensored)
                {
                    Debug.Log($"Path blocked by censored card at row {row}, col {col}");
                    continue; // Don't traverse through censored cards
                }
            }
            
            // Check if we're passing through a wrong exit (not at final column)
            if (currentCard.cardData.isEnd && col < gridZ - 1)
            {
                if (row != correctEndRow)
                {
                    Debug.LogWarning($"Path validation - wrong exit crossed at ({col}, {row})");
                    crossedWrongExit = true;
                }
            }
            
            // Check if we reached an exit at the final column
            if (col == gridZ - 1 && currentCard.cardData.isEnd)
            {
                if (row == correctEndRow)
                {
                    reachedCorrectExit = true;
                }
                else
                {
                    reachedWrongExit = true;
                }
                
                // Don't return immediately - continue exploring to find all exits
                continue;
            }

            // Check neighbors using cache - also check for blocking cards
            if (currentCard.ConnectsUp && row > 0 && playedCards[row - 1, col] != null)
            {
                if (cardCache.TryGetValue(playedCards[row - 1, col], out Card neighbor) && neighbor.ConnectsDown)
                {
                    queue.Enqueue((row - 1, col));
                }
            }
            if (currentCard.ConnectsDown && row < gridX - 1 && playedCards[row + 1, col] != null)
            {
                if (cardCache.TryGetValue(playedCards[row + 1, col], out Card neighbor) && neighbor.ConnectsUp)
                {
                    queue.Enqueue((row + 1, col));
                }
            }
            if (currentCard.ConnectsLeft && col > 0 && playedCards[row, col - 1] != null)
            {
                if (cardCache.TryGetValue(playedCards[row, col - 1], out Card neighbor) && neighbor.ConnectsRight)
                {
                    queue.Enqueue((row, col - 1));
                }
            }
            if (currentCard.ConnectsRight && col < gridZ - 1 && playedCards[row, col + 1] != null)
            {
                if (cardCache.TryGetValue(playedCards[row, col + 1], out Card neighbor) && neighbor.ConnectsLeft)
                {
                    queue.Enqueue((row, col + 1));
                }
            }
        }
        
        // After exploring all paths, determine result
        if (reachedCorrectExit)
        {
            if (crossedWrongExit)
            {
                Debug.Log("Path validation failed - crossed wrong exit before reaching correct exit");
                return (true, false);
            }
            Debug.Log("Path validation success - correct exit reached");
            return (true, true);
        }
        
        if (reachedWrongExit)
        {
            Debug.Log("Path validation failed - wrong exit reached");
            return (true, false);
        }

        return (false, false);
    }
    
    /// <summary>
    /// Check if the correct exit is still reachable from start.
    /// This checks two conditions:
    /// 1. The start card still has at least one open exit (can place cards)
    /// 2. There's a potential path from start to the correct exit (not fully blocked by barrier cards)
    /// Returns false if the player cannot possibly win.
    /// </summary>
    private bool IsCorrectExitReachable()
    {
        int startRow = gridX / 2;
        int startCol = 0;
        
        GameObject startCardObj = playedCards[startRow, startCol];
        if (startCardObj == null) return false;
        
        Card startCard = GetCachedCard(startCardObj);
        if (startCard == null) return false;
        
        // First check: Can we extend from start or any card connected to start?
        // Find all cells reachable from start via valid connections
        HashSet<(int row, int col)> reachableFromStart = GetCellsReachableFromStart();
        
        // Check if any reachable cell has an open exit to an empty slot
        bool canExtendPath = false;
        foreach (var (row, col) in reachableFromStart)
        {
            if (HasOpenExitToEmptySlot(row, col))
            {
                canExtendPath = true;
                break;
            }
        }
        
        // If we can't extend the path at all, check if we already reached the exit
        if (!canExtendPath)
        {
            // Check if we've already connected to the correct exit
            if (reachableFromStart.Contains((correctEndRow, gridZ - 1)))
            {
                return true; // We've reached the exit!
            }
            
            Debug.Log("IsCorrectExitReachable: No open exits from any reachable card - path is blocked!");
            return false;
        }
        
        // Second check: Can we potentially reach the correct exit via flood-fill?
        // (accounting for blocking cards)
        return CanPotentiallyReachExit(reachableFromStart);
    }
    
    /// <summary>
    /// Get all grid cells that are reachable from the start card via valid path connections.
    /// </summary>
    private HashSet<(int row, int col)> GetCellsReachableFromStart()
    {
        int startRow = gridX / 2;
        int startCol = 0;
        
        HashSet<(int row, int col)> visited = new HashSet<(int, int)>();
        Queue<(int row, int col)> queue = new Queue<(int, int)>();
        queue.Enqueue((startRow, startCol));
        
        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            if (visited.Contains((row, col))) continue;
            visited.Add((row, col));
            
            GameObject cardObj = playedCards[row, col];
            if (cardObj == null) continue;
            
            Card currentCard = GetCachedCard(cardObj);
            if (currentCard == null) continue;
            
            // Skip blocking cards (they break the path) - but not start/end cards
            if (!currentCard.cardData.isStart && !currentCard.cardData.isEnd)
            {
                if (currentCard.cardData.cardType == CardType.DeadEnd ||
                    currentCard.cardData.cardType == CardType.BureaucraticBarrier)
                {
                    continue;
                }
                
                if (currentCard.isCensored)
                {
                    continue;
                }
            }
            
            // Follow valid connections to neighboring cards
            if (currentCard.ConnectsUp && row > 0 && playedCards[row - 1, col] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row - 1, col]);
                if (neighbor != null && neighbor.ConnectsDown)
                {
                    queue.Enqueue((row - 1, col));
                }
            }
            
            if (currentCard.ConnectsDown && row < gridX - 1 && playedCards[row + 1, col] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row + 1, col]);
                if (neighbor != null && neighbor.ConnectsUp)
                {
                    queue.Enqueue((row + 1, col));
                }
            }
            
            if (currentCard.ConnectsLeft && col > 0 && playedCards[row, col - 1] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row, col - 1]);
                if (neighbor != null && neighbor.ConnectsRight)
                {
                    queue.Enqueue((row, col - 1));
                }
            }
            
            if (currentCard.ConnectsRight && col < gridZ - 1 && playedCards[row, col + 1] != null)
            {
                Card neighbor = GetCachedCard(playedCards[row, col + 1]);
                if (neighbor != null && neighbor.ConnectsLeft)
                {
                    queue.Enqueue((row, col + 1));
                }
            }
        }
        
        return visited;
    }
    
    /// <summary>
    /// Check if a card at the given position has an open exit leading to an empty slot.
    /// This means the player could potentially place a card there.
    /// </summary>
    private bool HasOpenExitToEmptySlot(int row, int col)
    {
        GameObject cardObj = playedCards[row, col];
        if (cardObj == null) return false;
        
        Card card = GetCachedCard(cardObj);
        if (card == null) return false;
        
        // Blocking cards don't have open exits
        if (!card.cardData.isStart && !card.cardData.isEnd)
        {
            if (card.cardData.cardType == CardType.DeadEnd ||
                card.cardData.cardType == CardType.BureaucraticBarrier)
            {
                return false;
            }
            
            if (card.isCensored)
            {
                return false;
            }
        }
        
        // Check each direction - if card connects that way AND the neighbor slot is empty
        if (card.ConnectsUp && row > 0 && playedCards[row - 1, col] == null)
        {
            return true;
        }
        
        if (card.ConnectsDown && row < gridX - 1 && playedCards[row + 1, col] == null)
        {
            return true;
        }
        
        if (card.ConnectsLeft && col > 0 && playedCards[row, col - 1] == null)
        {
            return true;
        }
        
        if (card.ConnectsRight && col < gridZ - 1 && playedCards[row, col + 1] == null)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if we can potentially reach the correct exit from any reachable cell.
    /// Uses flood-fill through empty spaces, blocked by DeadEnd/BureaucraticBarrier cards.
    /// </summary>
    private bool CanPotentiallyReachExit(HashSet<(int row, int col)> startingCells)
    {
        HashSet<(int row, int col)> visited = new HashSet<(int, int)>();
        Queue<(int row, int col)> queue = new Queue<(int, int)>();
        
        // Start from all reachable cells
        foreach (var cell in startingCells)
        {
            queue.Enqueue(cell);
        }
        
        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            if (visited.Contains((row, col))) continue;
            visited.Add((row, col));
            
            // Found the correct exit!
            if (row == correctEndRow && col == gridZ - 1)
            {
                return true;
            }
            
            // Check if this is a blocking card
            GameObject cardObj = playedCards[row, col];
            if (cardObj != null)
            {
                Card card = GetCachedCard(cardObj);
                if (card != null && !card.cardData.isStart && !card.cardData.isEnd)
                {
                    if (card.cardData.cardType == CardType.DeadEnd ||
                        card.cardData.cardType == CardType.BureaucraticBarrier)
                    {
                        continue; // Blocked - don't explore from here
                    }
                }
            }
            
            // Explore all 4 directions
            int[] dRows = { -1, 1, 0, 0 };
            int[] dCols = { 0, 0, -1, 1 };
            
            for (int i = 0; i < 4; i++)
            {
                int newRow = row + dRows[i];
                int newCol = col + dCols[i];
                
                // Bounds check
                if (newRow < 0 || newRow >= gridX || newCol < 0 || newCol >= gridZ)
                    continue;
                
                // Check if neighbor is blocked
                GameObject neighborObj = playedCards[newRow, newCol];
                if (neighborObj != null)
                {
                    Card neighbor = GetCachedCard(neighborObj);
                    if (neighbor != null && !neighbor.cardData.isStart && !neighbor.cardData.isEnd)
                    {
                        if (neighbor.cardData.cardType == CardType.DeadEnd ||
                            neighbor.cardData.cardType == CardType.BureaucraticBarrier)
                        {
                            continue; // Can't pass through blocking cards
                        }
                    }
                }
                
                queue.Enqueue((newRow, newCol));
            }
        }
        
        Debug.Log($"CanPotentiallyReachExit: Cannot reach exit at row {correctEndRow}, col {gridZ - 1}");
        return false;
    }
    
    /// <summary>
    /// Called when all paths to correct exit are blocked - Game Over
    /// </summary>
    private void OnGameOver()
    {
        Debug.Log("GAME OVER - All paths to correct exit are blocked");
        Debug.Log("Supervisor successfully blocked all escape routes");

        // Play supervisor win voice line
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.playSupervisorLossLine?.Invoke();
        }
    
        
        // Disable game updates
        enabled = false;
        
        // Show game over UI
        if (gameEndUI != null)
        {
            gameEndUI.ShowGameOver();
        }
        else
        {
            Debug.LogWarning("GameEndUI not assigned to GameManager!");
        }
    }
    
    /// <summary>
    /// Check if game should end due to blocked paths (call after Supervisor's turn)
    /// </summary>
    public void CheckForGameOver()
    {
        if (!IsCorrectExitReachable())
        {
            // Before declaring game over, check if player has any cards that could rescue the situation
            if (PlayerHasRescueCards())
            {
                Debug.Log("Path appears blocked, but player has rescue cards (Uncensor, Warrant, etc.) - game continues");
                return;
            }
            
            OnGameOver();
        }
    }
    
    /// <summary>
    /// Check if the player has any cards in hand OR in the draw pile that could potentially unblock the path.
    /// This includes Uncensor cards (to remove censorship) and Warrant cards (to remove barriers).
    /// </summary>
    private bool PlayerHasRescueCards()
    {
        // Check if there are censored cards blocking the path AND player has/can get Uncensor
        if (HasCensoredCardsBlockingPath())
        {
            if (PlayerHasCardOfType(CardType.Uncensor))
            {
                Debug.Log("Player has Uncensor card in hand - game continues");
                return true;
            }
            if (DrawPileHasCardOfType(CardType.Uncensor))
            {
                Debug.Log("Draw pile has Uncensor card available - game continues");
                return true;
            }
        }
        
        // Check if there are Bureaucratic Barriers blocking the path AND player has/can get Warrant
        if (HasBarriersBlockingPath())
        {
            if (PlayerHasCardOfType(CardType.Warrant))
            {
                Debug.Log("Player has Warrant card in hand - game continues");
                return true;
            }
            if (DrawPileHasCardOfType(CardType.Warrant))
            {
                Debug.Log("Draw pile has Warrant card available - game continues");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if there are any censored cards that are blocking potential paths to the exit.
    /// A censored card is "blocking" if uncensoring it could potentially open a path.
    /// </summary>
    private bool HasCensoredCardsBlockingPath()
    {
        // Get cells reachable from start (these are the cells we can currently reach)
        HashSet<(int row, int col)> reachableFromStart = GetCellsReachableFromStart();
        
        // Check all placed cards for censored ones adjacent to reachable cells
        for (int row = 0; row < gridX; row++)
        {
            for (int col = 0; col < gridZ; col++)
            {
                GameObject cardObj = playedCards[row, col];
                if (cardObj == null) continue;
                
                Card card = GetCachedCard(cardObj);
                if (card == null) continue;
                
                // Check if this card is censored
                if (card.isCensored)
                {
                    // Check if this censored card is adjacent to a reachable cell
                    // If so, uncensoring it might open up a new path
                    bool isAdjacentToReachable = 
                        (row > 0 && reachableFromStart.Contains((row - 1, col))) ||
                        (row < gridX - 1 && reachableFromStart.Contains((row + 1, col))) ||
                        (col > 0 && reachableFromStart.Contains((row, col - 1))) ||
                        (col < gridZ - 1 && reachableFromStart.Contains((row, col + 1)));
                    
                    if (isAdjacentToReachable)
                    {
                        Debug.Log($"Found censored card at row {row}, col {col} adjacent to reachable path");
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if there are any Bureaucratic Barriers blocking potential paths to the exit.
    /// </summary>
    private bool HasBarriersBlockingPath()
    {
        HashSet<(int row, int col)> reachableFromStart = GetCellsReachableFromStart();
        
        for (int row = 0; row < gridX; row++)
        {
            for (int col = 0; col < gridZ; col++)
            {
                GameObject cardObj = playedCards[row, col];
                if (cardObj == null) continue;
                
                Card card = GetCachedCard(cardObj);
                if (card == null || card.cardData == null) continue;
                
                // Check if this is a Bureaucratic Barrier
                if (card.cardData.cardType == CardType.BureaucraticBarrier)
                {
                    // Check if this barrier is adjacent to a reachable cell
                    bool isAdjacentToReachable = 
                        (row > 0 && reachableFromStart.Contains((row - 1, col))) ||
                        (row < gridX - 1 && reachableFromStart.Contains((row + 1, col))) ||
                        (col > 0 && reachableFromStart.Contains((row, col - 1))) ||
                        (col < gridZ - 1 && reachableFromStart.Contains((row, col + 1)));
                    
                    if (isAdjacentToReachable)
                    {
                        Debug.Log($"Found Bureaucratic Barrier at row {row}, col {col} adjacent to reachable path");
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if the player has a card of the specified type in their hand.
    /// </summary>
    private bool PlayerHasCardOfType(CardType cardType)
    {
        if (handController == null) return false;
        
        // Access the cards in hand via the handController's handPlaceHolder
        Transform handHolder = handController.handPlaceHolder;
        if (handHolder == null) return false;
        
        foreach (Transform child in handHolder)
        {
            Card card = child.GetComponent<Card>();
            if (card != null && card.cardData != null && card.cardData.cardType == cardType)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if the draw pile contains a card of the specified type.
    /// Also checks the discard pile since it gets reshuffled into the draw pile.
    /// </summary>
    private bool DrawPileHasCardOfType(CardType cardType)
    {
        // Check draw pile
        if (drawPile != null)
        {
            foreach (GameObject cardObj in drawPile.drawPile)
            {
                Card card = cardObj.GetComponent<Card>();
                if (card != null && card.cardData != null && card.cardData.cardType == cardType)
                {
                    return true;
                }
            }
        }
        
        // Also check discard pile since it gets reshuffled back into draw pile
        if (discardPile != null)
        {
            foreach (GameObject cardObj in discardPile.discardPile)
            {
                Card card = cardObj.GetComponent<Card>();
                if (card != null && card.cardData != null && card.cardData.cardType == cardType)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // Coroutine to check if player is looking away after placing a card
    private System.Collections.IEnumerator ExecuteSabotage()
    {
        isSabotageActive = true;
        
        // Check if the last player card still exists on the grid
        if (lastPlayerCard != null)
        {
            Card cardComponent = lastPlayerCard.GetComponent<Card>();
            if (cardComponent != null && cardComponent.cardData != null)
            {
                Debug.Log($"Sabotage executing - removing player card: {cardComponent.cardData.cardName}");
                yield return StartCoroutine(RemoveLastPlayedCard());
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.playSupervisorRemovalLine?.Invoke();
                }
            }
            else
            {
                Debug.LogWarning("Sabotage failed - player card is invalid");
            }
        }
        
        // Reset timer and flag
        lookingAwayTimer = 0f;
        isSabotageActive = false;
    }
    
    // Remove the last played card with animation
    private System.Collections.IEnumerator RemoveLastPlayedCard()
    {
        if (lastPlayerCard == null)
        {
            Debug.LogWarning("No last player card to remove!");
            yield break;
        }
        
        GameObject cardToRemove = lastPlayerCard;
        Vector2Int? cardPos = GetCardPosition(cardToRemove);
        
        if (!cardPos.HasValue)
        {
            Debug.LogWarning("Could not find position of last played card!");
            yield break;
        }
        
        int col = cardPos.Value.x;
        int row = cardPos.Value.y;
        
        Debug.Log($"Removing card at ({col}, {row})");
        
        // Store original state
        Vector3 startPosition = cardToRemove.transform.position;
        Quaternion startRotation = cardToRemove.transform.rotation;
        Vector3 startScale = cardToRemove.transform.localScale;
        
        // Calculate target position (discard pile location)
        Vector3 targetPosition = discardPile.discardAnchor.position;
        // Add offset for stacking
        targetPosition.y += discardPile.discardPile.Count * discardPile.cardStackOffset;
        
        Quaternion targetRotation = Quaternion.Euler(discardPile.discardPileRotation);
        Vector3 targetScale = discardPile.discardPileScale;
        
        // Get sprite renderer for potential color effects
        SpriteRenderer spriteRenderer = cardToRemove.GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        // Animate the card moving to discard pile
        float elapsedTime = 0f;
        
        while (elapsedTime < sabotageAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / sabotageAnimationDuration;
            
            // Use ease-in curve for more natural movement
            float easedT = t * t; // Quadratic ease-in
            
            // Move card to discard pile
            cardToRemove.transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            cardToRemove.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedT);
            cardToRemove.transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
            
            // Optional: slight fade during movement (makes it look smoother)
            if (spriteRenderer != null)
            {
                Color newColor = originalColor;
                newColor.a = Mathf.Lerp(1f, 0.7f, easedT); // Fade to 70% opacity
                spriteRenderer.color = newColor;
            }
            
            yield return null;
        }
        
        // Ensure final position is exact
        cardToRemove.transform.position = targetPosition;
        cardToRemove.transform.rotation = targetRotation;
        cardToRemove.transform.localScale = targetScale;
        
        // Restore full opacity
        if (spriteRenderer != null)
        {
            Color newColor = originalColor;
            newColor.a = 1f;
            spriteRenderer.color = newColor;
        }
        
        // Remove from grid
        playedCards[row, col] = null;
        cardPositions.Remove(cardToRemove);
        
        // Show grid slot as empty again
        gridSlotObjects[row, col]?.GetComponent<GridSlot>()?.ShowAsEmpty();
        
        // Add to discard pile (this will set parent, tag, etc.)
        discardPile.AddToDiscard(cardToRemove);
        
        // Clear last played card references
        lastPlayedCard = null;
        lastPlayerCard = null;
        
        Debug.Log("Card removed and discarded");
    }

    private void OnVictory()
    {
        Debug.Log("VICTORY! Player found the correct exit!");

        // Play supervisor loss voice line
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.playSupervisorWinLine?.Invoke();
        }
        
        // Disable game updates
        enabled = false;
        
        // Show victory UI
        if (gameEndUI != null)
        {
            gameEndUI.ShowVictory();
        }
        else
        {
            Debug.LogWarning("GameEndUI not assigned to GameManager!");
        }
    }
    
    private void OnWrongExit() { }

    private void ReceiveCallback(IAsyncResult ar)
    {
        // Early exit if udpClient was disposed
        if (udpClient == null) return;
        
        try
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            byte[] bytes = udpClient.EndReceive(ar, ref ip);
            string message = Encoding.UTF8.GetString(bytes);
            
            bool newState = message == "LOOKING_AWAY";
            
            // Only update and log if state actually changed
            if (newState != previousLookingAwayState)
            {
                lookingAway = newState;
                previousLookingAwayState = newState;
                Debug.Log(newState ? "LOOKING_AWAY state activated" : "LOOKING state activated");
            }
            
            if (udpClient != null)
            {
                udpClient.BeginReceive(ReceiveCallback, null);
            }
        }
        catch (ObjectDisposedException)
        {
            // Socket was closed, this is expected during shutdown
        }
        catch (SocketException)
        {
            // Socket error, likely no Python program running - this is fine
        }
        catch (Exception e)
        {
            // Only log if udpClient exists (avoid spam when Python isn't running)
            if (udpClient != null)
            {
                Debug.LogWarning($"UDP Warning: {e.Message}");
            }
        }
    }

    private void OnDisable()
    {
        CleanupUDP();
    }

    private void OnDestroy()
    {
        CleanupUDP();
    }

    private void OnApplicationQuit()
    {
        CleanupUDP();
    }
    
    private void CleanupUDP()
    {
        if (udpClient != null)
        {
            try
            {
                udpClient.Close();
                udpClient = null;
                Debug.Log("UDP Listener closed");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing UDP: {e.Message}");
            }
        }
    }

    // Additional properties for grid dimensions
    public int gridWidth => gridZ;
    public int gridHeight => gridX;
    
    /// <summary>
    /// Public method to re-validate the path after card state changes (e.g., uncensoring)
    /// Call this when a card's blocking state changes
    /// </summary>
    public void RevalidatePath()
    {
        Debug.Log("Re-validating path after card state change...");
        
        var pathResult = ValidatePath();
        if (pathResult.isComplete)
        {
            if (pathResult.isCorrect)
            {
                Debug.Log("Path complete after revalidation - CORRECT exit found!");
                OnVictory();
            }
            else
            {
                Debug.Log("Path complete after revalidation - WRONG exit reached!");
                OnWrongExit();
            }
        }
        else
        {
            Debug.Log("Path still incomplete after revalidation");
            // Optionally check for game over
            CheckForGameOver();
        }
    }
}