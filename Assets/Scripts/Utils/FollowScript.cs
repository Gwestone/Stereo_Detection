using UnityEngine;

public class FollowScript : MonoBehaviour
{
    [SerializeField] public Transform target;
    [SerializeField] public bool followRotation = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(target == null)
        {
            Debug.LogError("Target is not set");
        }
    }

    void LateUpdate()
    {
        if(target != null)
        {
            transform.position = target.position;
            if(followRotation)
            {
                transform.rotation = target.rotation;
            }
        }
    }
}
