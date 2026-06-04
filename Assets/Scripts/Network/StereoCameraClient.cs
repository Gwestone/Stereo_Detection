using UnityEngine;
using System.Net.Sockets;
using System.IO;
using UnityEngine.Rendering;
using System; // Added so we can catch Exceptions

public class StereoCameraClient : MonoBehaviour
{
    [Header("Cameras")]
    public Camera cameraLeft; // (Or Front/Rear depending on how you mounted them!)
    public Camera cameraRight;

    [Header("Network Settings")]
    public string targetIp = "127.0.0.1"; // Fixed the placeholder!
    public int targetPort = 5555;         // Make sure this matches your MATLAB script

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private BinaryWriter writer;
    private BinaryReader reader; // You don't technically need this unless MATLAB talks back, but it's fine to keep!

    protected void Start()
    {
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
    }

    protected void LateUpdate()
    {
        // SAFETY CHECK: Do not ask the GPU for data if the network is dead!
        if (this.tcpClient == null || !this.tcpClient.Connected) return;

        AsyncGPUReadback.Request(cameraLeft.targetTexture, 0, TextureFormat.RGB24, OnLeftCameraRequestComplete);
        AsyncGPUReadback.Request(cameraRight.targetTexture, 0, TextureFormat.RGB24, OnRightCameraRequestComplete);
    }

    void OnLeftCameraRequestComplete(AsyncGPUReadbackRequest request)
    {
        // SAFETY CHECK: Abort if the GPU failed or the socket closed while we were waiting
        if (request.hasError || !this.tcpClient.Connected)
        {
            Debug.LogError("Error while reading from left camera");
            return;
        }
        SendRawData(request.GetData<byte>().ToArray(), 1, cameraLeft.transform.rotation.eulerAngles);
    }

    void OnRightCameraRequestComplete(AsyncGPUReadbackRequest request)
    {
        if (request.hasError || !this.tcpClient.Connected)
        {
            Debug.LogError("Error while reading from right camera");
            return;
        }
        SendRawData(request.GetData<byte>().ToArray(), 2, cameraRight.transform.rotation.eulerAngles);
    }

    private void SendRawData(byte[] rawPixels, int eyeID, Vector3 eulerAngles)
    {
        try
        {
            this.writer.Write((byte)eyeID);
            this.writer.Write(eulerAngles.x);
            this.writer.Write(eulerAngles.y);
            this.writer.Write(eulerAngles.z);
            this.writer.Write(rawPixels, 0, rawPixels.Length);
            this.writer.Flush(); // Flush forces the data out immediately
        }
        catch { /* Ignore socket errors if MATLAB closes mid-stream */ }
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
