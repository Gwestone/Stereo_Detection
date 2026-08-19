using UnityEngine;

public class BulletScript : MonoBehaviour
{

    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.AddForce(transform.forward * 10_000f);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision){
        if (collision.contacts.Length > 0)
        {
            ContactPoint point = collision.GetContact(0);
            Debug.Log($"Hit: {collision.gameObject.name} at point: {point.point}");
        }
        if(collision.gameObject.tag == "Enemy")
        {
            Destroy(collision.gameObject);
        }
        Destroy(gameObject);
    }
}
