using UnityEngine;
using UnityEngine.Events;

public class DroneDetectEventListener : MonoBehaviour
{

    [SerializeField] private Radar radarFR;
    [SerializeField] private Radar radarFL;
    [SerializeField] private Radar radarBR;
    [SerializeField] private Radar radarBL;

    protected void Start()
    {
        radarFR.OnObjectDetected.AddListener(OnDetect);
        radarFL.OnObjectDetected.AddListener(OnDetect);
        radarBR.OnObjectDetected.AddListener(OnDetect);
        radarBL.OnObjectDetected.AddListener(OnDetect);
    }

    protected void OnDestroy()
    {
        radarFR.OnObjectDetected.RemoveListener(OnDetect);
        radarFL.OnObjectDetected.RemoveListener(OnDetect);
        radarBR.OnObjectDetected.RemoveListener(OnDetect);
        radarBL.OnObjectDetected.RemoveListener(OnDetect);
    }

    public void OnDetect(DroneDetectEventData data)
    {
        Debug.Log("Detection event received from radar: " + data.id);
    }

}
