using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class DroneDetectEventListener : MonoBehaviour
{

    [SerializeField] private Radar radarFR;
    [SerializeField] private Radar radarFL;
    [SerializeField] private Radar radarMR;
    [SerializeField] private Radar radarML;
    [SerializeField] private Radar radarBR;
    [SerializeField] private Radar radarBL;

    private Queue<DroneDetectEventData> _detectQueue = new Queue<DroneDetectEventData>();

    protected TcpClient tcpClient;
    protected NetworkStream tcpStream;
    protected BinaryWriter bufferWriter;
    protected BinaryReader bufferReader;
    private uint sequenceCounter = 0;

    protected void Start()
    {
        radarFR.OnObjectDetected.AddListener(OnDetect);
        radarFL.OnObjectDetected.AddListener(OnDetect);
        radarMR.OnObjectDetected.AddListener(OnDetect);
        radarML.OnObjectDetected.AddListener(OnDetect);
        radarBR.OnObjectDetected.AddListener(OnDetect);
        radarBL.OnObjectDetected.AddListener(OnDetect);

        try
        {
            tcpClient = new TcpClient("localhost", 7000);
            tcpStream = tcpClient.GetStream();
            bufferWriter = new BinaryWriter(tcpStream);
            bufferReader = new BinaryReader(tcpStream);

            bufferWriter.Write((byte)0x00);
            bufferWriter.Flush();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Could not connect to server. Is PYTHON running? Error: " + e.Message);
        }
    }

    protected void OnDestroy()
    {
        radarFR.OnObjectDetected.RemoveListener(OnDetect);
        radarFL.OnObjectDetected.RemoveListener(OnDetect);
        radarMR.OnObjectDetected.RemoveListener(OnDetect);
        radarML.OnObjectDetected.RemoveListener(OnDetect);
        radarBR.OnObjectDetected.RemoveListener(OnDetect);
        radarBL.OnObjectDetected.RemoveListener(OnDetect);
    }

    public void OnDetect(DroneDetectEventData data)
    {
        _detectQueue.Enqueue(data);
    }

    protected void FixedUpdate()
    {
        while (_detectQueue.Count > 0)
        {
            var data = _detectQueue.Dequeue();
            Debug.Log("Detection event received from radar: " + data.id);
            ulong tNs = (ulong)(Stopwatch.GetTimestamp() * (1_000_000_000.0 / Stopwatch.Frequency));

            Quaternion bodyRotation = data.bodyRot;
            Quaternion directionRotation = data.directionRot;

            if (bufferWriter != null)
            {
                lock (tcpStream)
                {
                    bufferWriter.Write((byte)0x00);
                    bufferWriter.Write((byte)data.id);
                    bufferWriter.Write((float)data.distance);
                    bufferWriter.Write((float)directionRotation.x);
                    bufferWriter.Write((float)directionRotation.y);
                    bufferWriter.Write((float)directionRotation.z);
                    bufferWriter.Write((float)directionRotation.w);
                    bufferWriter.Write((ulong)tNs);
                    bufferWriter.Write((float)bodyRotation.x);
                    bufferWriter.Write((float)bodyRotation.y);
                    bufferWriter.Write((float)bodyRotation.z);
                    bufferWriter.Write((float)bodyRotation.w);
                    bufferWriter.Flush();
                }
            }
        }
    }

}
