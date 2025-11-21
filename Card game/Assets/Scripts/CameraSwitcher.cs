using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera[] cameras; // Your 3 cameras
    [SerializeField] private CinemachineCamera fpsCamera;
    [SerializeField] private GameObject fpsPlayer;
    
    [SerializeField] private Transform cardPlaceholder;
    [SerializeField] private Vector3[] cardPositions; // corresponding positions for each camera
    [SerializeField] private Vector3[] cardRotations; // corresponding rotations for each camera
    
    [SerializeField] private int currentCameraIndex = 1;
    private bool inFPSMode = false;
    
    void Start()
    {
        ActivateCamera(currentCameraIndex);
        UpdateCardPlaceholder();
        fpsPlayer.SetActive(false);
    }

    void Update()
    {
            if (inFPSMode) return;
        
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                // Move forward only if not at last camera
                if (currentCameraIndex < cameras.Length - 1)
                {
                    currentCameraIndex++;
                    ActivateCamera(currentCameraIndex);
                    UpdateCardPlaceholder();
                }
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                // Move backward only if not at first camera
                if (currentCameraIndex > 0)
                {
                    currentCameraIndex--;
                    ActivateCamera(currentCameraIndex);
                    UpdateCardPlaceholder();
                }
            }
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                EnterFPSMode();
            }
            
    }

    private void ActivateCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].Priority = (i == index) ? 10 : 0;
        }
    }
    private void EnterFPSMode()
    {
        inFPSMode = true;

        fpsPlayer.SetActive(true);
        fpsCamera.Priority = 20; 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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