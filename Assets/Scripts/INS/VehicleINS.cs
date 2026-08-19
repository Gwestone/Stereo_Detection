using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class VehicleINS : MonoBehaviour
{

    [SerializeField] private Transform mount;   // optional; defaults to root
    void Awake() { if (mount == null) mount = transform; }

    protected TcpClient tcpClient;
    protected NetworkStream tcpStream;
    protected BinaryWriter bufferWriter;
    protected BinaryReader bufferReader;

    private Rigidbody rb;
    private Vector3 lastVelocity;
    ulong ns = 0;

    // This property holds the current acceleration vector
    public Vector3 CurrentAcceleration { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastVelocity = rb.linearVelocity;

        try
        {
            tcpClient = new TcpClient("localhost", 7000);
            tcpStream = tcpClient.GetStream();
            bufferWriter = new BinaryWriter(tcpStream);
            bufferReader = new BinaryReader(tcpStream);

            bufferWriter.Write((byte)0x02);
            bufferWriter.Flush();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Could not connect to server. Is PYTHON running? Error: " + e.Message);
        }
    }

    void FixedUpdate()
    {
        // 1. Calculate the change in velocity
        Vector3 deltaVelocity = rb.linearVelocity - lastVelocity;
        // 2. Divide by FixedDeltaTime to get acceleration (units/s^2)
        CurrentAcceleration = deltaVelocity / Time.fixedDeltaTime;
        ns += (ulong)((double)Time.fixedDeltaTime * 1_000_000_000.0D);
        // 3. Save the current velocity for the next frame
        lastVelocity = rb.linearVelocity;

        Quaternion Attitude = mount.rotation;
        Vector3 Position = mount.position;

        if (bufferWriter != null){
            bufferWriter.Write((float)Attitude.x);
            bufferWriter.Write((float)Attitude.y);
            bufferWriter.Write((float)Attitude.z);
            bufferWriter.Write((float)Attitude.w);
            bufferWriter.Write((ulong)ns);
            bufferWriter.Write((float)CurrentAcceleration.x);
            bufferWriter.Write((float)CurrentAcceleration.y);
            bufferWriter.Write((float)CurrentAcceleration.z);
            bufferWriter.Flush();
        }
    }
}
