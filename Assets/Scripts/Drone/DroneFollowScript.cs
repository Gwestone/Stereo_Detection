using UnityEngine;

public class DroneFollowScript : MonoBehaviour
{
    [Header("Movement Limits")]
    [SerializeField] public float maxSpeed = 33f;
    [SerializeField] public GameObject player;
    
    [Tooltip("How fast the drone can rotate its nose (Degrees per Second).")]
    [SerializeField] private float turnRateDegrees = 120f;
    
    [Tooltip("Engine power: How fast it speeds up straight ahead (m/s^2).")]
    [SerializeField] private float maxForwardAccel = 25f;
    
    [Tooltip("Air Grip: How hard it fights sliding sideways. Low = Drifty, High = Missile (m/s^2).")]
    [SerializeField] private float maxLateralAccel = 15f;

    [Header("Avoidance Settings")]
    [SerializeField] private float groundRadarDistance = 20f; 
    [SerializeField] private float dodgeForce = 300f;        
    [SerializeField] private float verticalDamping = 10f;  

    [Header("Forces")]
    [SerializeField] private float hoverForceMultiplier = 1f;

    [HideInInspector] public Rigidbody playerRb;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (player != null) playerRb = player.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (player == null || playerRb == null) return;

        // 1. Prediction math (Unchanged)
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float currentMaxSpeed = (distanceToPlayer > 30f) ? maxSpeed * 1.5f : maxSpeed;

        Vector3 futurePlayerPos = transform.position + DroneMath.CalculateInterceptDirection(transform.position, player.transform.position, playerRb.linearVelocity, currentMaxSpeed, 1.2f);
        Vector3 directionToPlayer = (futurePlayerPos - transform.position).normalized;

        // --- NEW: STRICT TURN RATE LIMITING ---
        // Instead of Slerp, we use RotateTowards to guarantee it never turns faster than allowed
        float singleStep = turnRateDegrees * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 newForward = Vector3.RotateTowards(transform.forward, directionToPlayer, singleStep, 0.0f);
        transform.rotation = Quaternion.LookRotation(newForward);

        // --- NEW: G-FORCE CLAMPED PHYSICS ---
        // The drone wants to go where its nose is pointing
        Vector3 desiredVelocity = transform.forward * currentMaxSpeed;
        
        // Calculate the raw acceleration needed to fix the velocity error in 0.5 seconds
        Vector3 requiredAcceleration = (desiredVelocity - rb.linearVelocity) * 2f; 

        // Split that acceleration into "Forward" and "Sideways" components
        float forwardRequest = Vector3.Dot(requiredAcceleration, transform.forward);
        Vector3 lateralRequestVector = requiredAcceleration - (transform.forward * forwardRequest);

        // CLAMP to our physical limits!
        float clampedForward = Mathf.Clamp(forwardRequest, -maxForwardAccel, maxForwardAccel);
        Vector3 clampedLateral = Vector3.ClampMagnitude(lateralRequestVector, maxLateralAccel);

        // Recombine and apply mass
        Vector3 totalForce = ((transform.forward * clampedForward) + clampedLateral) * rb.mass;

        // --- Dive-Bomb Override & Radar (Unchanged) ---
        float groundFearWeight = Mathf.Clamp01(distanceToPlayer / 20f);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundRadarDistance))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                float panicLevel = 1f - (hit.distance / groundRadarDistance);
                float currentUpwardSpeed = rb.linearVelocity.y;
                float dampingForce = Mathf.Max(0, currentUpwardSpeed * verticalDamping);

                float finalUpwardForce = ((dodgeForce * panicLevel) - dampingForce) * groundFearWeight;
                finalUpwardForce = Mathf.Max(0, finalUpwardForce);

                totalForce += Vector3.up * finalUpwardForce;
                Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.red);
            }
        }
        else
        {
            Debug.DrawRay(transform.position, Vector3.down * groundRadarDistance, Color.green);
        }
 
        Vector3 hoverForce = Vector3.up * 9.81f * rb.mass * hoverForceMultiplier * groundFearWeight;
        rb.AddForce(totalForce + hoverForce);
    }

    void OnCollisionEnter(Collision collision)
    {
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

// --- The "Functional Core" ---
// This is a static class. It holds no data, it remembers nothing.
// It just takes inputs and spits out perfect, predictable math.
public static class DroneMath
{
    public static Vector3 CalculateInterceptDirection(
        Vector3 dronePos, 
        Vector3 targetPos, 
        Vector3 targetVelocity, 
        float droneMaxSpeed, 
        float maxPredictionTime = 1.2f) // Added a time cap!
    {
        // 1. Calculate ideal time to intercept
        float distance = Vector3.Distance(dronePos, targetPos);
        float timeToIntercept = distance / droneMaxSpeed;

        // --- NEW: HORIZON CLAMPING ---
        // Never predict further ahead than our max limit (e.g., 1.2 seconds)
        // This stops the drone from getting confused if you brake from far away
        timeToIntercept = Mathf.Min(timeToIntercept, maxPredictionTime);

        // --- NEW: CLOSE-RANGE BLENDING ---
        // If distance is 15+, weight is 1 (Full Prediction). 
        // If distance is 0, weight is 0 (Direct Pursuit).
        float predictionWeight = Mathf.Clamp01(distance / 15f);

        // 3. Calculate the smart aim point
        Vector3 futureOffset = targetVelocity * timeToIntercept * predictionWeight;
        Vector3 predictedPosition = targetPos + futureOffset;

        return (predictedPosition - dronePos).normalized;
    }
}