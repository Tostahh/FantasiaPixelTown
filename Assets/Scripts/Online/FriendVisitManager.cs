using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.IO;
using System;

public class FriendVisitManager : MonoBehaviour
{
    public static FriendVisitManager Instance { get; private set; }

    private NetworkDriver driver;
    private NetworkConnection connection;
    private VisitConnectionHandler connectionHandler;
    private bool isRunning;
    private bool isHost;

    [SerializeField] private ushort port = 7777;
    public string currentSaveFile = "current_save.json";
    [SerializeField] private string spectatorSaveFile = "spectator_save.json";
    public string CurrentFriendCode { get; private set; }
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!isRunning || !driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        if (isHost)
            AcceptConnections();
        else
            connectionHandler?.PollEvents();
    }

    public void StartHosting()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.AnyIpv4; // or NetworkEndpoint.LoopbackIpv4 for local testing
        endpoint.Port = port;

        if (driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind port " + port);
            driver.Dispose();
            return;
        }

        driver.Listen();
        isHost = true;
        isRunning = true;
        Debug.Log("Hosting on port " + port);
    }

    public void JoinHost(string ip)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        driver = NetworkDriver.Create();
        connection = default;

        if (NetworkEndpoint.TryParse(ip, port, out NetworkEndpoint endpoint))
        {
            connection = driver.Connect(endpoint);
            connectionHandler = new VisitConnectionHandler(driver, connection, OnDataReceived, OnConnectedToHost, OnDisconnected);
            isHost = false;
            isRunning = true;
            Debug.Log("Joining host at " + ip + ":" + port);
        }
        else
        {
            Debug.LogError("Invalid IP address: " + ip);
        }
    }

    private void AcceptConnections()
    {
        NetworkConnection incoming;
        while ((incoming = driver.Accept()) != default)
        {
            connection = incoming;
            Debug.Log("Visitor connected!");
            connectionHandler = new VisitConnectionHandler(driver, connection, OnDataReceived);
            SendSaveFileToVisitor();
        }
    }

    private void SendSaveFileToVisitor()
    {
        string path = Path.Combine(Application.persistentDataPath, currentSaveFile);
        if (!File.Exists(path))
        {
            Debug.LogWarning("No save file found to send.");
            return;
        }

        byte[] bytes = File.ReadAllBytes(path);

        // prepend length
        byte[] dataWithLength = new byte[4 + bytes.Length];
        BitConverter.GetBytes(bytes.Length).CopyTo(dataWithLength, 0);
        bytes.CopyTo(dataWithLength, 4);

        using (var nativeArray = new NativeArray<byte>(dataWithLength, Allocator.Temp))
        {
            driver.BeginSend(connection, out var sendHandle);
            sendHandle.WriteBytes(nativeArray);
            driver.EndSend(sendHandle);
        }

        Debug.Log("Sent save file (" + bytes.Length + " bytes).");
    }


    private void OnDataReceived(DataStreamReader reader)
    {
        // Read length (first 4 bytes)
        int length = reader.ReadInt();
        byte[] data = new byte[length];
        reader.ReadBytes(data);

        string dest = Path.Combine(Application.persistentDataPath, spectatorSaveFile);
        File.WriteAllBytes(dest, data);

        Debug.Log($"[FriendVisitManager] Received save file ? {dest}");
        AutoLoadSpectatorSave(dest);
    }


    private void OnConnectedToHost() => Debug.Log("Connected to host");
    private void OnDisconnected() => Debug.LogWarning("Disconnected from host");

    private void AutoLoadSpectatorSave(string path)
    {
        SaveManager.Instance.LoadFromFile(path);
    }

    public void SetCurrentSaveFromManager()
    {
        if (SaveManager.Instance?.CurrentSave == null)
        {
            Debug.LogWarning("[FriendVisitManager] No current save loaded.");
            return;
        }

        currentSaveFile = Path.Combine(Application.persistentDataPath, $"slot_{SaveManager.Instance.activeSlot}_save.json");

        // Optionally auto-save so the file is up to date
        SaveManager.Instance.SaveGame();
    }

    public void SetFriendCode(string code)
    {
        CurrentFriendCode = code;
    }

    private void OnApplicationQuit()
    {
        StopHostingOrDisconnect();
    }

    public void StopHostingOrDisconnect()
    {
        if (isHost)
        {
            Debug.Log("Shutting down host...");
        }
        else
        {
            Debug.Log("Disconnecting from host...");
        }

        if (connection.IsCreated)
            connection.Disconnect(driver);

        connectionHandler = null;

        if (driver.IsCreated)
        {
            driver.Dispose();
            Debug.Log("Network driver disposed.");
        }

        isRunning = false;
        isHost = false;
    }

}
