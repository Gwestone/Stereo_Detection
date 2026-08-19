using UnityEngine;
using System.Net.Sockets;
using System.IO;
using UnityEngine.Rendering;
using System; // Added so we can catch Exceptions
using System.Threading;
using System.Collections.Concurrent;

public struct TurretCommandStruct
{
    public float azimuthalAngle;
    public float polarAngle;
    public bool secondLenseActive;
}

public class TurretController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera firstCamera;

    [Header("Network Settings")]
    public string targetIp = "127.0.0.1"; // Fixed the placeholder!
    public int targetPort = 5555;         // Make sure this matches your MATLAB script

    [Header("Turret Hinges")]
    [SerializeField] public Transform bearingHingeY;
    [SerializeField] public Transform gunHingeZ;
    [SerializeField] public Transform cameraHingeZ;

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private BinaryWriter writer;
    private BinaryReader reader; // You don't technically need this unless MATLAB talks back, but it's fine to keep!
    private RenderTexture rt;

    private Thread readerThread;
    private volatile bool running;
    private readonly ConcurrentQueue<TurretCommandStruct> commands = new();

    protected void Start()
    {
        rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        firstCamera.targetTexture = rt;

        try
        {
            this.tcpClient = new TcpClient();
            this.tcpClient.Connect(this.targetIp, this.targetPort);
            this.networkStream = this.tcpClient.GetStream();
            this.writer = new BinaryWriter(this.networkStream);
            this.reader = new BinaryReader(this.networkStream);
            Debug.Log("Connected to MATLAB!");
        }
        catch (Exception e)
        {
            // If MATLAB isn't running, this stops Unity from breaking
            Debug.LogError("Could not connect to server. Is MATLAB running? Error: " + e.Message);
        }

        this.running = true;
        this.readerThread = new Thread(this.ReadLoop) { IsBackground = true };
        this.readerThread.Start();
    }

    protected void FixedUpdate()
    {
        while (commands.TryDequeue(out TurretCommandStruct command))
        {

            float azimuthalAngle = command.azimuthalAngle;
            float polarAngle = Math.Clamp(command.polarAngle, 0, 85);

            bearingHingeY.localRotation = Quaternion.Euler(0, azimuthalAngle, 0);
            gunHingeZ.localRotation = Quaternion.Euler(0, 0, polarAngle);
            cameraHingeZ.localRotation = Quaternion.Euler(0, 0, polarAngle);
        }
    }

    protected void LateUpdate()
    {
        // SAFETY CHECK: Do not ask the GPU for data if the network is dead!
        if (this.tcpClient == null || !this.tcpClient.Connected) return;

        AsyncGPUReadback.Request(firstCamera.targetTexture, 0, TextureFormat.RGB24, OnLeftCameraRequestComplete);
    }

    void OnLeftCameraRequestComplete(AsyncGPUReadbackRequest request)
    {
        // SAFETY CHECK: Abort if the GPU failed or the socket closed while we were waiting
        if (request.hasError || !this.tcpClient.Connected)
        {
            Debug.LogError("Error while reading from left camera");
            return;
        }
        SendRawData(request.GetData<byte>().ToArray(), 1, firstCamera.transform.rotation.eulerAngles);
    }

    private void SendRawData(byte[] rawPixels, int eyeID, Vector3 eulerAngles)
    {
        try
        {
            this.writer.Write((byte)eyeID);
            this.writer.Write(eulerAngles.y);
            this.writer.Write(eulerAngles.x);
            this.writer.Write(eulerAngles.z);
            this.writer.Write(rawPixels, 0, rawPixels.Length);
            this.writer.Flush(); // Flush forces the data out immediately
        }
        catch { /* Ignore socket errors if MATLAB closes mid-stream */ }
    }

    void ReadLoop() {
        while (running) {
            try {
                Debug.Log("Reading...");
                if (reader.ReadUInt32() != 0x03) continue;   // blocks; that's fine here
                commands.Enqueue(new TurretCommandStruct {
                    azimuthalAngle = reader.ReadSingle(),
                    polarAngle = reader.ReadSingle(),
                    secondLenseActive = reader.ReadBoolean(),
                });
            } catch { break; }        // socket closed
        }
    }

    protected void OnDestroy()
    {
        // OnDestroy is safer than OnApplicationQuit, as it handles when the script is disabled or object destroyed
        if (this.writer != null) this.writer.Close();
        if (this.reader != null) this.reader.Close();
        if (this.networkStream != null) this.networkStream.Close();
        if (this.tcpClient != null) this.tcpClient.Close();
    }

}
