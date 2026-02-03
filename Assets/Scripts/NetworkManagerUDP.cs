using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NetworkManagerUDP : MonoBehaviour
{
    [Header("Role Settings")]
    public bool isServer = true; // PC端勾选，手机端不勾选

    [Header("Network Settings")]
    public string targetIP = "127.0.0.1"; // 手机端填PC的IP，调试填127.0.0.1
    public int port = 9050;

    private UdpClient udpClient;
    private Thread receiveThread;
    private string lastReceivedData = "";
    private string lastReceivedRotation = ""; // 分开存储旋转数据
    private string lastProcessedData = ""; // 记录已处理的数据
    private object lockObject = new object();

    // 单例模式方便调用
    public static NetworkManagerUDP Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (isServer)
        {
            StartServer();
        }
        else
        {
            StartClient();
        }
    }

    private void StartServer()
    {
        try
        {
            udpServer = new UdpClient(port);
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log($"[Network] Server started on port {port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Network] Server start failed: {e.Message}");
        }
    }

    private UdpClient udpServer;

    private void StartClient()
    {
        udpClient = new UdpClient();
        Debug.Log($"[Network] Client ready to send to {targetIP}:{port}");
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                byte[] data = udpServer.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                lock (lockObject)
                {
                    if (message.StartsWith("ACTION:"))
                    {
                        lastReceivedData = message;
                    }
                    else if (message.StartsWith("ROT:"))
                    {
                        lastReceivedRotation = message;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Network] Receive error: {e.Message}");
                break;
            }
        }
    }

    public bool SendData(string message)
    {
        if (isServer || udpClient == null) return false;
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, targetIP, port);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Network] Send error: {e.Message}");
            return false;
        }
    }

    public string GetLastData()
    {
        lock (lockObject)
        {
            return lastReceivedData;
        }
    }

    public string GetLastRotation()
    {
        lock (lockObject)
        {
            return lastReceivedRotation;
        }
    }

    // 新增：获取并标记为已处理，防止重复触发 Action
    public string ConsumeLastAction()
    {
        lock (lockObject)
        {
            if (lastReceivedData.StartsWith("ACTION:") && lastReceivedData != lastProcessedData)
            {
                lastProcessedData = lastReceivedData;
                return lastReceivedData;
            }
            return null;
        }
    }

    void OnApplicationQuit()
    {
        if (udpServer != null) udpServer.Close();
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
}