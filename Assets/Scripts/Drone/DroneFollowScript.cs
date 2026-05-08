using UnityEngine;

public class DroneFollowScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float maxSpeed = 10;
    [SerializeField] public GameObject player;
    [Tooltip("How fast the drone can physically rotate. Lower = heavier feel.")]
    [SerializeField] private float turnSpeed = 5f;

    [Header("Avoidance Settings")]
    [SerializeField] private float groundRadarDistance = 20f; 
    [SerializeField] private float dodgeForce = 300f;        
    [SerializeField] private float verticalDamping = 10f; 

    [Header("Forces")]
    [SerializeField] private float hoverForceMultiplier = 1f;
    [SerializeField] public float maneuverability = 5f; 

    private Rigidbody playerRb;
    private Rigidbody rb;

    void Start()
    {
        playerRb = player.GetComponent<Rigidbody>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (player == null || playerRb == null) return;

        // 1. Where do we WANT to look?
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float currentMaxSpeed = (distanceToPlayer > 30f) ? maxSpeed * 1.5f : maxSpeed;

        Vector3 futurePlayerPos = transform.position + DroneMath.CalculateInterceptDirection(transform.position, player.transform.position, playerRb.linearVelocity, currentMaxSpeed, 1.2f);
        Vector3 directionToPlayer = (futurePlayerPos - transform.position).normalized;

        // --- NEW: SMOOTH ROTATION ---
        // Instead of instantly LookAt, we smoothly rotate towards the target over time
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        // ----------------------------

        // --- UPDATED: LOOK-DRIVEN PHYSICS ---
        // The drone now wants to go wherever its nose is CURRENTLY pointing, not magically at the player.
        // If it hasn't finished turning yet, it will swing wide!
        Vector3 desiredVelocity = transform.forward * currentMaxSpeed;
        
        Vector3 velocityError = desiredVelocity - rb.linearVelocity;
        Vector3 totalForce = (velocityError * maneuverability * rb.mass);
        // ------------------------------------

        // --- Dive-Bomb Override ---
        float groundFearWeight = Mathf.Clamp01(distanceToPlayer / 20f);

        // --- Ground Avoidance Radar ---
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

                Vector3 emergencyThrust = Vector3.up * finalUpwardForce;
                totalForce += emergencyThrust;
                
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