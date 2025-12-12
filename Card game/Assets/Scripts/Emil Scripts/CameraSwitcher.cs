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
    [SerializeField] private CinemachineCamera[] cameras; // 2 cameras
    
    [Header("Card Placeholder")]
    [SerializeField] private Transform cardPlaceholder;
    [SerializeField] private Vector3[] cardPositions; // 2 positions: Normal and Board
    [SerializeField] private Vector3[] cardRotations; // 2 rotations: Normal and Board
    
    [Header("Animation Settings")]
    [SerializeField] private float cardAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve cardAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Settings")]
    [SerializeField] private int currentCameraIndex = 0; // Start at NormalView (index 0)
    
    private bool isTransitioning = false;
    private Coroutine currentTransition;

    void Start()
    {
        // Start with the camera at currentCameraIndex
        ActivateCamera(currentCameraIndex);
        
        // Set card placeholder to starting position immediately
        if (cardPlaceholder != null)
        {
            cardPlaceholder.localPosition = cardPositions[currentCameraIndex];
            cardPlaceholder.localEulerAngles = cardRotations[currentCameraIndex];
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Don't allow switching while transitioning
        if (isTransitioning) return;

        // W key - Switch to BoardView
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            if (currentCameraIndex == 0) // If at NormalView, go to BoardView
            {
                StartCameraTransition(1);
            }
        }

        // S key - Switch to NormalView
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (currentCameraIndex == 1) // If at BoardView, go to NormalView
            {
                StartCameraTransition(0);
            }
        }
        
        // Optional: Toggle between views with Tab
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            int newIndex = (currentCameraIndex == 0) ? 1 : 0;
            StartCameraTransition(newIndex);
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

        // Switch camera and animate cards SIMULTANEOUSLY
        ActivateCamera(newIndex);
        yield return StartCoroutine(AnimateCards(oldIndex, newIndex));

        currentCameraIndex = newIndex;
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
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].Priority = (i == index) ? 10 : 0;
        }
    }
    
    public int GetCurrentCameraIndex()
    {
        return currentCameraIndex;
    }
    
    public void SwitchToCamera(int index)
    {
        if (isTransitioning) return;
        
        if (index >= 0 && index < cameras.Length && index != currentCameraIndex)
        {
            StartCameraTransition(index);
        }
    }
    
    public bool IsNormalView()
    {
        return currentCameraIndex == 0;
    }
    
    public bool IsBoardView()
    {
        return currentCameraIndex == 1;
    }
}