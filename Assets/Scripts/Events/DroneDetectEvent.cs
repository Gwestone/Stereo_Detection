using UnityEngine;
using UnityEngine.Events;

public struct DroneDetectEventData
{
    public uint id;
    public Vector3 direction;
    public float confidence;
    public float distance;
}

public class DroneDetectEvent : UnityEvent<DroneDetectEventData>
{
    public void Raise(DroneDetectEventData data)
    {
        Invoke(data);
    }

    public static DroneDetectEventData GetFromTransform(uint index, Transform targetTransform, Transform bodyTransform)
    {
        Vector3 directionVec = targetTransform.position - bodyTransform.position;
        float confidence = bodyTransform.localScale.magnitude;
        float distance = Vector3.Distance(targetTransform.position, bodyTransform.position);

        return new DroneDetectEventData
        {
            id = index,
            direction = (directionVec + Random.insideUnitSphere).normalized,
            confidence = confidence + Random.Range(-0.1f, 0.1f),
            distance = distance + Random.Range(-1f, 1f),
        };
    }
}
