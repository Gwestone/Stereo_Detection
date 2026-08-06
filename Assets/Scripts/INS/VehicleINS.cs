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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
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

    // Update is called once per frame
    protected void Update()
    {
        Quaternion Attitude = mount.rotation;
        Vector3 Position = mount.position;

        bufferWriter.Write((float)Attitude.x);
        bufferWriter.Write((float)Attitude.y);
        bufferWriter.Write((float)Attitude.z);
        bufferWriter.Write((float)Attitude.w);

    }
}
