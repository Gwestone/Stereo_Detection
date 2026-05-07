using UnityEngine;
using Unity.Cinemachine; 
using UnityEngine.InputSystem; // <-- Required for the New Input System

public class CameraZoom : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineCamera virtualCamera; 
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 1.5f; 
    public float minZoom = 1.5f;   
    public float maxZoom = 8f;     

    private Cinemachine3rdPersonFollow thirdPersonFollow;

    void Start()
    {
        if (virtualCamera != null)
        {
            thirdPersonFollow = virtualCamera.GetComponent<Cinemachine3rdPersonFollow>();
        }
        else
        {
            Debug.LogWarning("Missing Camera reference on the CameraZoom script!");
        }
    }

    void Update()
    {
        if (thirdPersonFollow == null) return;

        // Ensure there is actually a mouse connected before trying to read it
        if (Mouse.current == null) return;

        // Read the mouse wheel using the New Input System
        // We divide by 120f to normalize the value, keeping the zoom speed consistent
        float scrollInput = Mouse.current.scroll.ReadValue().y / 120f;

        if (scrollInput != 0)
        {
            // Calculate and apply the new distance
            float newDistance = thirdPersonFollow.CameraDistance - (scrollInput * zoomSpeed);
            thirdPersonFollow.CameraDistance = Mathf.Clamp(newDistance, minZoom, maxZoom);
        }
    }
}