using UnityEngine;

public class DroneFollowScript : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10;
    [SerializeField] private GameObject player;

    private Vector3 velocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;
    private Rigidbody rb;
    private Vector3 liftForce = Vector3.zero; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        liftForce = Vector3.up * 9.81f * rb.mass;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = Vector3.MoveTowards(transform.position, player.transform.position, maxSpeed * Time.deltaTime);
        transform.LookAt(player.transform);
    }

    void FixedUpdate()
    {
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        this.acceleration = directionToPlayer * this.maxSpeed;

        rb.AddForce(this.acceleration * rb.mass + liftForce);

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
            Destroy(gameObject);
        }
    }
}
