using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{

    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider backLeft;
    public WheelCollider backRight;

    [Header("Car Settings")]
    public float motorForce = 1500f; // How strong the engine is
    public float maxSteerAngle = 30f; // How far the wheels can turn

    private float horizontalInput;
    private float verticalInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        frontLeft.transform.localRotation = Quaternion.Euler(0, 0, 90);
        frontRight.transform.localRotation = Quaternion.Euler(0, 0, 90);
        backLeft.transform.localRotation = Quaternion.Euler(0, 0, 90);
        backRight.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    // Update is called once per frame
    void Update()
    {
        // Reset inputs every frame
        horizontalInput = 0f;
        verticalInput = 0f;

        // 2. Safely check if a keyboard exists, then read the WASD keys
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput = -1f;
            
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
        }
    }
 
    void FixedUpdate()
    {
        // 2. Apply steering to the FRONT wheels
        float steer = maxSteerAngle * horizontalInput;
        frontLeft.steerAngle = steer;
        frontRight.steerAngle = steer;

        // 3. Apply engine power to the BACK wheels (Rear-Wheel Drive)
        float acceleration = motorForce * verticalInput;
        backLeft.motorTorque = acceleration;
        backRight.motorTorque = acceleration;
        
        // Note: If you want All-Wheel Drive (AWD), just apply motorTorque to the front wheels too!
    }
}
