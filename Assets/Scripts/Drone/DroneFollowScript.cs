using UnityEngine;

public class DroneFollowScript : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10;
    [SerializeField] private GameObject player;

    private Vector3 velocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

        this.velocity += this.acceleration * Time.fixedDeltaTime;
        this.velocity = Vector3.ClampMagnitude(this.velocity, this.maxSpeed);

        transform.position += this.velocity * Time.fixedDeltaTime;;
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
