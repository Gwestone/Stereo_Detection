using UnityEngine;

public class CarCenterOfMass : MonoBehaviour
{
    [Tooltip("Push this value into the negatives to lower the weight into the floor")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0); 
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // This overrides Unity's calculated center of mass with our custom low point
        rb.centerOfMass = centerOfMassOffset;
    }
    
    // Optional: Draws a green sphere in the editor so you can see where the weight is!
    void OnDrawGizmos()
    {
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.1f);
        }
    }
}