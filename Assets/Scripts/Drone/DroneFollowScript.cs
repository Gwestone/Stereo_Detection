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
    // A Pure Function: No side effects, no hidden state.
    public static Vector3 CalculateInterceptDirection(
        Vector3 dronePos, 
        Vector3 targetPos, 
        Vector3 targetVelocity, 
        float droneMaxSpeed, 
        float predictionTimeMultiplier)
    {
        // Calculate time to intercept
        float distance = Vector3.Distance(dronePos, targetPos);
        float timeToIntercept = distance / droneMaxSpeed;

        // Calculate where the target will be
        Vector3 futureOffset = targetVelocity * timeToIntercept * predictionTimeMultiplier;
        Vector3 predictedPosition = targetPos + futureOffset;

        // Return the direction to that future point
        return (predictedPosition - dronePos).normalized;
    }
}