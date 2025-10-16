using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

public class FriendVisitManager : MonoBehaviour
{
    public static FriendVisitManager Instance { get; private set; }

    private NetworkDriver driver;
    private NetworkPipeline reliablePipeline;
    private NetworkPipeline fragmentedReliablePipeline;

    private List<NetworkConnection> activeConnections = new();
    private NetworkConnection clientConnection;

    private bool isHost;
    private bool isRunning;

    [SerializeField] private ushort port = 7777;
    [SerializeField] private string spectatorSaveFile = "spectator_save.json";

    private string currentFriendCode;
    public string CurrentFriendCode => currentFriendCode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!isRunning || !driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        if (isHost)
        {
            AcceptConnections();
            ReceiveFromVisitors();
        }
        else
        {
            ReceiveFromHost();
        }
    }

    // -------------------- HOST --------------------

    public void SetFriendCode(string code) => currentFriendCode = code;

    public void StartHosting()
    {
        SaveManager.Instance?.SaveGame();

        if (isRunning) StopHostingOrDisconnect();

        var settings = new NetworkSettings();
        settings.WithNetworkConfigParameters(
            receiveQueueCapacity: 64,
            sendQueueCapacity: 64,
            maxMessageSize: 1400 // <= Required for UDP MTU compliance
        );

        driver = NetworkDriver.Create(settings);

        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        fragmentedReliablePipeline = driver.CreatePipeline(
            typeof(FragmentationPipelineStage),
            typeof(ReliableSequencedPipelineStage)
        );

        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
        if (driver.Bind(endpoint) != 0)
        {
            Debug.LogError($"[FriendVisitManager] Failed to bind to port {port}");
            driver.Dispose();
            return;
        }

        driver.Listen();
        isHost = true;
        isRunning = true;

        Debug.Log($"[FriendVisitManager] Hosting on port {port}");
    }

    private void AcceptConnections()
    {
        NetworkConnection incoming;
        while ((incoming = driver.Accept()) != default)
        {
            activeConnections.Add(incoming);
            Debug.Log("[FriendVisitManager] Visitor connected");
            SendSaveFileToVisitor(incoming);
        }
    }

    private void ReceiveFromVisitors()
    {
        for (int i = activeConnections.Count - 1; i >= 0; i--)
        {
            var conn = activeConnections[i];
            DataStreamReader reader;

            NetworkEvent.Type evt;
            while ((evt = conn.PopEvent(driver, out reader)) != NetworkEvent.Type.Empty)
            {
                if (evt == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("[FriendVisitManager] Visitor disconnected");
                    activeConnections.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void SendSaveFileToVisitor(NetworkConnection conn)
    {
        if (SaveManager.Instance?.CurrentSave == null)
        {
            Debug.LogWarning("[FriendVisitManager] No save data to send.");
            return;
        }

        string json = JsonUtility.ToJson(SaveManager.Instance.CurrentSave, true);
        byte[] fullData = Encoding.UTF8.GetBytes(json);

        Debug.Log($"[FriendVisitManager] Sending save file ({fullData.Length} bytes) to visitor");

        using var nativeData = new NativeArray<byte>(fullData, Allocator.Temp);
        driver.BeginSend(fragmentedReliablePipeline, conn, out var writer);
        writer.WriteBytes(nativeData);
        driver.EndSend(writer);
    }

    // -------------------- CLIENT --------------------

    public void JoinHost(string ip)
    {
        if (isRunning) StopHostingOrDisconnect();

        var settings = new NetworkSettings();
        settings.WithNetworkConfigParameters(
            receiveQueueCapacity: 64,
            sendQueueCapacity: 64,
            maxMessageSize: 1400
        );

        driver = NetworkDriver.Create(settings);

        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        fragmentedReliablePipeline = driver.CreatePipeline(
            typeof(FragmentationPipelineStage),
            typeof(ReliableSequencedPipelineStage)
        );

        if (!NetworkEndpoint.TryParse(ip, port, out var endpoint))
        {
            Debug.LogError($"[FriendVisitManager] Invalid IP: {ip}");
            return;
        }

        clientConnection = driver.Connect(endpoint);
        isHost = false;
        isRunning = true;

        Debug.Log($"[FriendVisitManager] Joining host at {ip}:{port}");
    }

    private List<byte> receiveBuffer = new();

    private void ReceiveFromHost()
    {
        if (!clientConnection.IsCreated) return;

        DataStreamReader reader;
        NetworkEvent.Type evt;
        while ((evt = clientConnection.PopEvent(driver, out reader)) != NetworkEvent.Type.Empty)
        {
            switch (evt)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("[FriendVisitManager] Connected to host");
                    break;

                case NetworkEvent.Type.Data:
                    ReadSaveData(reader);
                    break;

                case NetworkEvent.Type.Disconnect:
                    Debug.LogWarning("[FriendVisitManager] Disconnected from host");
                    clientConnection = default;
                    break;
            }
        }
    }

    private void ReadSaveData(DataStreamReader reader)
    {
        if (reader.Length <= 0) return;

        byte[] bytes = new byte[reader.Length];
        reader.ReadBytes(bytes);

        receiveBuffer.AddRange(bytes);
        Debug.Log($"[FriendVisitManager] Received {bytes.Length} bytes (total {receiveBuffer.Count})");

        // FragmentationPipeline automatically reassembles, so we get the full file in one piece.
        string dest = Path.Combine(Application.persistentDataPath, spectatorSaveFile);
        File.WriteAllBytes(dest, receiveBuffer.ToArray());
        Debug.Log($"[FriendVisitManager] Full save file received ({receiveBuffer.Count} bytes) -> {dest}");
        receiveBuffer.Clear();

        AutoLoadSpectatorSave(dest);
    }

    private void AutoLoadSpectatorSave(string path)
    {
        try
        {
            SaveManager.Instance?.LoadFromFile(path);
            Debug.Log($"[FriendVisitManager] Spectator save loaded from {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FriendVisitManager] Failed to load spectator save: {e.Message}");
        }
    }

    // -------------------- CLEANUP --------------------

    private void OnApplicationQuit() => StopHostingOrDisconnect();

    public void StopHostingOrDisconnect()
    {
        if (!isRunning) return;

        if (isHost)
            Debug.Log("[FriendVisitManager] Shutting down host...");
        else
            Debug.Log("[FriendVisitManager] Disconnecting from host...");

        foreach (var conn in activeConnections)
        {
            if (conn.IsCreated)
                conn.Disconnect(driver);
        }
        activeConnections.Clear();

        if (clientConnection.IsCreated)
            clientConnection.Disconnect(driver);

        if (driver.IsCreated)
        {
            driver.Dispose();
            Debug.Log("[FriendVisitManager] Network driver disposed");
        }

        isRunning = false;
        isHost = false;
    }
}
