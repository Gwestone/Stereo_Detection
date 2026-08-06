using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneFollowScript : MonoBehaviour
{
    [Header("Movement Limits")]
    public float maxSpeed = 33f;
    public GameObject player;

    [Tooltip("How fast the drone can rotate its nose (Degrees per Second).")]
    [SerializeField] private float turnRateDegrees = 120f;

    [Tooltip("Engine power: How fast it speeds up straight ahead (m/s^2).")]
    [SerializeField] private float maxForwardAccel = 25f;

    [Tooltip("Air Grip: How hard it fights sliding sideways (m/s^2).")]
    [SerializeField] private float maxLateralAccel = 15f;

    [Header("Orbit Settings")]
    [Tooltip("How far away the drone should stay while circling.")]
    [SerializeField] private float orbitRadius = 25f;
    [Tooltip("True for Clockwise, False for Counter-Clockwise")]
    [SerializeField] private bool orbitClockwise = true;

    [Header("Avoidance Settings")]
    [SerializeField] private float groundRadarDistance = 20f;
    [SerializeField] private float dodgeForce = 50f;
    [SerializeField] private float verticalDamping = 10f;

    [Header("Forces")]
    [SerializeField] private float hoverForceMultiplier = 1f;

    [HideInInspector] public Rigidbody playerRb;
    private Rigidbody rb;

    protected void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (player != null) playerRb = player.GetComponent<Rigidbody>();
    }

    protected void FixedUpdate()
    {
        if (player == null) return;

        // --- 1. ROTATION (Steering for Orbit) ---
        Vector3 directionToOrbit = DroneMath.CalculateOrbitDirection(
            transform.position,
            player.transform.position,
            orbitRadius,
            orbitClockwise
        );

        float singleStep = turnRateDegrees * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 newForward = Vector3.RotateTowards(transform.forward, directionToOrbit, singleStep, 0.0f);
        transform.rotation = Quaternion.LookRotation(newForward);

        // --- 2. MOVEMENT ACCELERATION ---
        Vector3 velocityError = (transform.forward * maxSpeed) - rb.linearVelocity;

        float forwardError = Vector3.Dot(velocityError, transform.forward);
        Vector3 lateralErrorVector = velocityError - (transform.forward * forwardError);

        float clampedForward = Mathf.Clamp(forwardError, -maxForwardAccel, maxForwardAccel);
        Vector3 clampedLateral = Vector3.ClampMagnitude(lateralErrorVector, maxLateralAccel);

        Vector3 movementAccel = (transform.forward * clampedForward) + clampedLateral;

        // --- 3. GROUND AVOIDANCE & HOVER ---
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float groundFearWeight = Mathf.Clamp01(distanceToPlayer / 20f);

        Vector3 verticalAccel = Vector3.zero;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundRadarDistance))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                float panicLevel = 1f - (hit.distance / groundRadarDistance);
                float currentDownwardSpeed = Mathf.Max(0, -rb.linearVelocity.y);
                float dampingForce = currentDownwardSpeed * verticalDamping;

                float finalUpwardForce = ((dodgeForce * panicLevel) + dampingForce) * groundFearWeight;
                verticalAccel += Vector3.up * finalUpwardForce;

                Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.red);
            }
        }
        else
        {
            Debug.DrawRay(transform.position, Vector3.down * groundRadarDistance, Color.green);
        }

        // Counteract standard Unity gravity (9.81)
        verticalAccel += Vector3.up * (9.81f * hoverForceMultiplier * groundFearWeight);

        // --- 4. APPLY FORCES ---
        rb.AddForce(movementAccel + verticalAccel, ForceMode.Acceleration);
    }

    protected void OnCollisionEnter(Collision collision)
    {
        // Optional: You might want to disable this now so it doesn't blow up
        // if the player accidentally bumps into it while it orbits.
        if (collision.gameObject == player)
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}

public static class DroneMath
{
    public static Vector3 CalculateOrbitDirection(Vector3 dronePos, Vector3 targetPos, float targetRadius, bool clockwise)
    {
        // 1. Find vector from drone to target
        Vector3 vectorToTarget = targetPos - dronePos;
        float currentDistance = vectorToTarget.magnitude;
        Vector3 dirToTarget = vectorToTarget.normalized;

        // 2. Calculate Tangent (The circle path) using Cross Product
        // Cross product with Vector3.up gives us a perfectly horizontal tangent vector
        Vector3 tangent;
        if (clockwise)
            tangent = Vector3.Cross(Vector3.up, dirToTarget).normalized;
        else
            tangent = Vector3.Cross(dirToTarget, Vector3.up).normalized;

        // 3. Maintain Radius (Push/Pull)
        // If we are too far, distanceError is positive. Too close, it's negative.
        float distanceError = currentDistance - targetRadius;

        // Divide by 5f to create a smooth blending zone.
        // Clamping to -1 and 1 ensures it doesn't overpower the tangent completely.
        float pullFactor = Mathf.Clamp(distanceError / 5f, -1f, 1f);

        // 4. Combine the circle path with the push/pull correction
        return (tangent + (dirToTarget * pullFactor)).normalized;
    }
}
