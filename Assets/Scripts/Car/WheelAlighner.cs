using UnityEngine;

public class WheelAlighner : MonoBehaviour
{
    [Tooltip("The invisible physics wheel")]
    public WheelCollider wheelCollider; 
    
    [Tooltip("The 3D model of the wheel (the cylinder)")]
    public Transform visualWheel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Safety check to make sure both slots are filled
        if (wheelCollider == null || visualWheel == null) return;

        // 1. Create variables to hold the position and rotation
        Vector3 position;
        Quaternion rotation;

        // 2. Ask the WheelCollider for its exact bouncy position and rotation
        wheelCollider.GetWorldPose(out position, out rotation);

        // 3. Force the 3D cylinder to match that position and rotation
        visualWheel.position = position;
        visualWheel.rotation = rotation;   
    }
}
