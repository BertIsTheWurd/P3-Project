using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.Serialization;

public class CameraSwitcher : MonoBehaviour
{
    // These events are the communication hooks.
    // Action<int> means the event broadcasts a message containing one integer value (the index).
    public static event Action<int> OnActiveCameraChanged;
    public static event Action<int> OnActiveCardChanged;

    [SerializeField] private CinemachineCamera[] cameras; // The 3 cameras
    [SerializeField] private CinemachineCamera fpsCamera;
    // Array to hold all your card GameObjects
    [SerializeField] private GameObject[] heldCardObjects; 
    // Index to track which card is currently visible
    [SerializeField] private int currentCardIndex = 0;
    [SerializeField] private GameObject fpsPlayer;
    
    [SerializeField] private Transform cardPlaceholder;
    [SerializeField] private Vector3[] cardPositions; // corresponding positions for each camera
    [SerializeField] private Vector3[] cardRotations; // corresponding rotations for each camera
    
    [SerializeField] private int currentCameraIndex = 1;
    private bool inFPSMode = false;
    
    void Start()
    {
        // Start in fixed camera mode
        ActivateCamera(currentCameraIndex);
        UpdateCardPlaceholder();
        
        // Ensure FPS player starts hidden
        fpsPlayer.SetActive(false);
        
        // Ensure cursor is visible/unlocked at start
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Check for the 'F' key press to toggle modes
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (inFPSMode)
            {
                ExitFPSMode(); // Transition back to fixed cameras
            }
            else
            {
                EnterFPSMode(); // Transition to FPS camera
            }
            // We return here to prevent any W/S processing during the frame we switch modes
            return;
        }
        // Only allow W/S movement when NOT in FPS mode
        if (inFPSMode) return;
        
        // -- Fixed Camera Cycling Logic --
        
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            // Move forward (higher index) only if not at last camera
            if (currentCameraIndex < cameras.Length - 1)
            {
                currentCameraIndex++;
                ActivateCamera(currentCameraIndex);
                UpdateCardPlaceholder();
                // Broadcast the new camera index:
                OnActiveCameraChanged?.Invoke(currentCameraIndex); 
            }
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            // Move backward (lower index) only if not at first camera
            if (currentCameraIndex > 0)
            {
                currentCameraIndex--;
                ActivateCamera(currentCameraIndex);
                UpdateCardPlaceholder();
                // Broadcast the new camera index:
                OnActiveCameraChanged?.Invoke(currentCameraIndex);
            }
        }
        
    }

    private void ActivateCamera(int index)
    {
        // Set the active fixed camera to Priority 10, all others to 0
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].Priority = (i == index) ? 10 : 0;
        }
    }
    private void EnterFPSMode()
    {
        inFPSMode = true;
        
        // FPS Camera gets higher priority (20) to take over the MainCamera
        fpsCamera.Priority = 20; 
        
        // Activate player movement and set cursor state
        fpsPlayer.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitFPSMode()
    {
        inFPSMode = false;
        
        // Lower the FPS priority back to 0
        // This tells the Cinemachine Brain to switch to the next highest priority camera (the fixed one at Priority 10)
        fpsCamera.Priority = 0;
        
        // Deactivate player movement and restore cursor state
        fpsPlayer.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateCardPlaceholder()
    {
        if (cardPlaceholder != null && cardPositions.Length == cameras.Length && cardRotations.Length == cameras.Length)
        {
            cardPlaceholder.localPosition = cardPositions[currentCameraIndex];
            cardPlaceholder.localEulerAngles = cardRotations[currentCameraIndex];
        }
    }
}