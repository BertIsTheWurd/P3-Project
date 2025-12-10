using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class CameraSwitcher : MonoBehaviour
{
    // Event to notify other scripts when the camera changes
    public static event Action<int> OnActiveCameraChanged;

    [Header("Camera Setup")]
    [SerializeField] private CinemachineCamera[] cameras; // The 3 cameras
    
    [Header("Card Placeholder")]
    [SerializeField] private Transform cardPlaceholder;
    [SerializeField] private Vector3[] cardPositions; // corresponding positions for each camera
    [SerializeField] private Vector3[] cardRotations; // corresponding rotations for each camera
    
    [Header("Animation Settings")]
    [SerializeField] private float cardAnimationDuration = 0.5f; // How long the card animation takes
    [SerializeField] private AnimationCurve cardAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float cameraBlendTime = 1.0f; // How long camera blends take (set in Cinemachine Brain)
    
    [Header("Timing Settings")]
    [SerializeField] private bool animateCardsBeforeCameraSwitch = true; // Cards move first, then camera
    [SerializeField] private float cardDelayBeforeCamera = 0.3f; // Delay before camera starts switching (if cards go first)
    [SerializeField] private float cardDelayAfterCamera = 0.3f; // Delay before cards start moving (if camera goes first)
    
    [Header("Settings")]
    [SerializeField] private int currentCameraIndex = 1; // Start at NormalView (index 1)
    
    private bool isTransitioning = false;
    private Coroutine currentTransition;

    void Start()
    {
        // Start with the camera at currentCameraIndex
        ActivateCamera(currentCameraIndex);
        
        // Set card placeholder to starting position immediately (no animation)
        if (cardPlaceholder != null)
        {
            cardPlaceholder.localPosition = cardPositions[currentCameraIndex];
            cardPlaceholder.localEulerAngles = cardRotations[currentCameraIndex];
        }
        
        // Ensure cursor is visible/unlocked at start
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Don't allow switching while transitioning
        if (isTransitioning) return;

        // W key - Move forward through cameras (Hand -> Normal -> Board)
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            if (currentCameraIndex < cameras.Length - 1)
            {
                int newIndex = currentCameraIndex + 1;
                StartCameraTransition(newIndex);
            }
        }

        // S key - Move backward through cameras (Board -> Normal -> Hand)
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (currentCameraIndex > 0)
            {
                int newIndex = currentCameraIndex - 1;
                StartCameraTransition(newIndex);
            }
        }
    }

    private void StartCameraTransition(int newIndex)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        currentTransition = StartCoroutine(TransitionToCamera(newIndex));
    }

    private IEnumerator TransitionToCamera(int newIndex)
    {
        isTransitioning = true;
        int oldIndex = currentCameraIndex;
        
        Debug.Log($"Transitioning from camera {oldIndex} to camera {newIndex}");

        // Determine transition direction
        bool movingForward = newIndex > oldIndex; // HandView -> NormalView -> BoardView
        
        // FORWARD TRANSITIONS (HandView -> Normal -> Board)
        if (movingForward)
        {
            // HandView to NormalView: Cards move down first, then camera switches
            if (oldIndex == 0 && newIndex == 1)
            {
                yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
                yield return new WaitForSeconds(cardDelayBeforeCamera);
                ActivateCamera(newIndex);
            }
            // NormalView to BoardView: Camera switches first, then cards come up
            else if (oldIndex == 1 && newIndex == 2)
            {
                ActivateCamera(newIndex);
                yield return new WaitForSeconds(cardDelayAfterCamera);
                yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
            }
            // Generic forward transition
            else
            {
                if (animateCardsBeforeCameraSwitch)
                {
                    yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
                    yield return new WaitForSeconds(cardDelayBeforeCamera);
                    ActivateCamera(newIndex);
                }
                else
                {
                    ActivateCamera(newIndex);
                    yield return new WaitForSeconds(cardDelayAfterCamera);
                    yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
                }
            }
        }
        // BACKWARD TRANSITIONS (BoardView -> Normal -> Hand)
        else
        {
            // BoardView to NormalView: Cards move down first, then camera switches
            if (oldIndex == 2 && newIndex == 1)
            {
                yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
                yield return new WaitForSeconds(cardDelayBeforeCamera);
                ActivateCamera(newIndex);
            }
            // NormalView to HandView: Camera switches first, then cards come up
            else if (oldIndex == 1 && newIndex == 0)
            {
                ActivateCamera(newIndex);
                yield return new WaitForSeconds(cardDelayAfterCamera);
                yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
            }
            // Generic backward transition
            else
            {
                if (animateCardsBeforeCameraSwitch)
                {
                    yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
                    yield return new WaitForSeconds(cardDelayBeforeCamera);
                    ActivateCamera(newIndex);
                }
                else
                {
                    ActivateCamera(newIndex);
                    yield return new WaitForSeconds(cardDelayAfterCamera);
                    yield return StartCoroutine(AnimateCards(oldIndex, newIndex));
                }
            }
        }

        currentCameraIndex = newIndex;
        
        // Broadcast the new camera index to other scripts
        OnActiveCameraChanged?.Invoke(currentCameraIndex);
        
        isTransitioning = false;
        Debug.Log($"Transition complete. Now at camera {currentCameraIndex}");
    }

    private IEnumerator AnimateCards(int fromIndex, int toIndex)
    {
        if (cardPlaceholder == null) yield break;

        Vector3 startPos = cardPositions[fromIndex];
        Vector3 endPos = cardPositions[toIndex];
        Vector3 startRot = cardRotations[fromIndex];
        Vector3 endRot = cardRotations[toIndex];

        float elapsed = 0f;

        while (elapsed < cardAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cardAnimationDuration;
            float curveT = cardAnimationCurve.Evaluate(t);

            cardPlaceholder.localPosition = Vector3.Lerp(startPos, endPos, curveT);
            cardPlaceholder.localEulerAngles = Vector3.Lerp(startRot, endRot, curveT);

            yield return null;
        }

        // Ensure final position is exact
        cardPlaceholder.localPosition = endPos;
        cardPlaceholder.localEulerAngles = endRot;
    }

    private void ActivateCamera(int index)
    {
        // Set the active camera to Priority 10, all others to 0
        // Cinemachine Brain will automatically blend between cameras
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].Priority = (i == index) ? 10 : 0;
        }
    }
    
    // Public method to get current camera index (useful for other scripts)
    public int GetCurrentCameraIndex()
    {
        return currentCameraIndex;
    }
    
    // Public method to switch to a specific camera by index
    public void SwitchToCamera(int index)
    {
        if (isTransitioning) return;
        
        if (index >= 0 && index < cameras.Length && index != currentCameraIndex)
        {
            StartCameraTransition(index);
        }
        else if (index == currentCameraIndex)
        {
            Debug.Log($"Already at camera {index}");
        }
        else
        {
            Debug.LogWarning($"CameraSwitcher: Invalid camera index {index}");
        }
    }
}