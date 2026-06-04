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

    [Header("Avoidance Settings")]
    [SerializeField] private float groundRadarDistance = 20f;
    [SerializeField] private float dodgeForce = 50f; // Note: Lowered because mass is no longer multiplied
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

        // --- 1. ROTATION (Steering) ---
        Vector3 directionToPlayer = DroneMath.CalculateInterceptDirection(
            transform.position,
            player.transform.position,
            playerRb.linearVelocity,
            maxSpeed
        );

        float singleStep = turnRateDegrees * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 newForward = Vector3.RotateTowards(transform.forward, directionToPlayer, singleStep, 0.0f);
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
                float currentDownwardSpeed = Mathf.Max(0, -rb.linearVelocity.y); // Simplified to one line
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
        // ForceMode.Acceleration ignores mass entirely, meaning our limits act as pure m/s^2
        rb.AddForce(movementAccel + verticalAccel, ForceMode.Acceleration);
    }

    protected void OnCollisionEnter(Collision collision)
    {
        /*if (collision.gameObject == player)
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
            }*/
    }
}

public static class DroneMath
{
    public static Vector3 CalculateInterceptDirection(
        Vector3 dronePos, Vector3 targetPos, Vector3 targetVelocity,
        float droneMaxSpeed, float maxPredictionTime = 1.2f)
    {
        float distance = Vector3.Distance(dronePos, targetPos);
        float timeToIntercept = Mathf.Min(distance / droneMaxSpeed, maxPredictionTime);
        float predictionWeight = Mathf.Clamp01(distance / 15f);

        Vector3 futureOffset = targetVelocity * (timeToIntercept * predictionWeight);

        // Simplified return math (removed the redundant extra vector creation)
        return (targetPos + futureOffset - dronePos).normalized;
    }
}
