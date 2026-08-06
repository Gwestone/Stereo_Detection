using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public struct DroneDetectEventData
{
    public uint id;
    public Quaternion directionRot;
    public float distance;
    public Quaternion bodyRot;
}

public class DroneDetectEvent : UnityEvent<DroneDetectEventData>
{
    public void Raise(DroneDetectEventData data)
    {
        Invoke(data);
    }

    public static DroneDetectEventData GetFromTransform(uint index, Transform targetTransform, Transform bodyTransform)
    {
        Vector3 directionVec = (targetTransform.position - bodyTransform.position).normalized;
        Quaternion absDirectionQuat = Quaternion.LookRotation(directionVec, Vector3.up);
        Quaternion relDirectionQuat = Quaternion.Inverse(bodyTransform.rotation) * absDirectionQuat;

        // rel = dody^-1 * absDirection
        // absDirection = body * rel

        float distance = Vector3.Distance(targetTransform.position, bodyTransform.position);

        return new DroneDetectEventData
        {
            id = index,
            distance = distance,
            directionRot = relDirectionQuat,
            //INS sytem readings, use body trasform for radar for now
            bodyRot = bodyTransform.rotation
        };
    }
}
