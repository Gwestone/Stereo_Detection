using UnityEngine;

public class DroneFollowScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 10;
    [SerializeField] private GameObject player;

    [Header("Avoidance Settings")]
    [SerializeField] private float groundRadarDistance = 20f; // How far down the laser looks
    [SerializeField] private float dodgeForce = 300f;        // How hard it pushes up to survive

    [Header("References")]
    public Rigidbody playerRb;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Always look at the player
        if (player == null) return;
        
        Vector3 futurePlayerPos = transform.position + DroneMath.CalculateInterceptDirection(transform.position, player.transform.position, playerRb.linearVelocity, maxSpeed, 1.2f);

        // 1. Calculate direction to player
        Vector3 directionToPlayer = (futurePlayerPos - transform.position).normalized;

        transform.LookAt(directionToPlayer);
        
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 futurePlayerPos = transform.position + DroneMath.CalculateInterceptDirection(transform.position, player.transform.position, playerRb.linearVelocity, maxSpeed, 1.2f);

        // 1. Calculate direction to player
        Vector3 directionToPlayer = (futurePlayerPos - transform.position).normalized;
        Vector3 acceleration = directionToPlayer * maxSpeed;

        // 2. Base movement (Hover + Move to player)
        Vector3 totalForce = (acceleration * rb.mass);

        // --- NEW: Ground Avoidance Radar ---
        // 3. Shoot an invisible ray straight down from the drone
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundRadarDistance))
        {
            // Did the laser hit the ground?
            if (hit.collider.CompareTag("Ground"))
            {
                // Calculate panic level. (1 = crashing right now, 0.1 = just entered radar)
                float panicLevel = 1f - (hit.distance / groundRadarDistance);
                
                // Push the drone straight up based on how close the ground is
                Vector3 emergencyThrust = Vector3.up * dodgeForce * panicLevel;
                totalForce += emergencyThrust;
                
                // Optional: Draw a red line in the editor so you can see the radar working!
                Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.red);
            }
        }
        else
        {
            // Optional: Draw a green line when the ground is safely far away
            Debug.DrawRay(transform.position, Vector3.down * groundRadarDistance, Color.green);
        }
 
        // 4. Apply all forces
        rb.AddForce(totalForce);

        // 5. Speed Limit
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
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
            Destroy(gameObject); // It will still explode if it hits the ground!
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