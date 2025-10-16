using System;
using System.Collections;
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

    private List<NetworkConnection> activeConnections = new();
    private NetworkConnection clientConnection;

    private bool isHost;
    private bool isRunning;

    [SerializeField] private ushort port = 7777;
    [SerializeField] private string spectatorSaveFile = "spectator_save.json";

    private string currentFriendCode;

    // Keep packets under 1400 bytes for Unity Transport UDP MTU
    private const int CHUNK_SIZE = 1200;

    private Dictionary<int, byte[]> receivedChunks = new();
    private int expectedChunks = -1;

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
            receiveQueueCapacity: 128,
            sendQueueCapacity: 128,
            maxMessageSize: 1400 // Must stay under UDP MTU
        );

        driver = NetworkDriver.Create(settings);
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

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
            StartCoroutine(SendSaveFileInChunks(incoming));
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

    private IEnumerator SendSaveFileInChunks(NetworkConnection conn)
    {
        if (SaveManager.Instance?.CurrentSave == null)
        {
            Debug.LogWarning("[FriendVisitManager] No save data to send.");
            yield break;
        }

        string json = JsonUtility.ToJson(SaveManager.Instance.CurrentSave, true);
        byte[] fullData = Encoding.UTF8.GetBytes(json);

        int totalChunks = Mathf.CeilToInt(fullData.Length / (float)CHUNK_SIZE);
        Debug.Log($"[FriendVisitManager] Sending save file ({fullData.Length} bytes) in {totalChunks} chunks");

        for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            if (!conn.IsCreated)
            {
                Debug.LogWarning("[FriendVisitManager] Connection lost mid-send");
                yield break;
            }

            int offset = chunkIndex * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, fullData.Length - offset);

            using var chunk = new NativeArray<byte>(length, Allocator.Temp);
            NativeArray<byte>.Copy(fullData, offset, chunk, 0, length);

            driver.BeginSend(reliablePipeline, conn, out var writer);
            writer.WriteInt(chunkIndex);   // chunk index
            writer.WriteInt(totalChunks);  // total chunk count
            writer.WriteBytes(chunk);
            driver.EndSend(writer);

            Debug.Log($"[FriendVisitManager] Sent chunk {chunkIndex + 1}/{totalChunks} ({length} bytes)");

            // Yield every few chunks to avoid buffer overflow
            if (chunkIndex % 5 == 0)
                yield return null;
        }

        Debug.Log("[FriendVisitManager] Finished sending save file.");
    }

    // -------------------- CLIENT --------------------

    public void JoinHost(string ip)
    {
        if (isRunning) StopHostingOrDisconnect();

        var settings = new NetworkSettings();
        settings.WithNetworkConfigParameters(
            receiveQueueCapacity: 128,
            sendQueueCapacity: 128,
            maxMessageSize: 1400
        );

        driver = NetworkDriver.Create(settings);
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

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

    private void ReceiveFromHost()
    {
        if (!clientConnection.IsCreated)
        {
            Debug.LogError("[FriendVisitManager] ClientConnectionNotMade");
            return;
        }

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
        if (reader.Length <= 0)
        {
            Debug.LogError("Received empty chunk");
            return;
        }

        int chunkIndex = reader.ReadInt();
        int totalChunks = reader.ReadInt();

        if (expectedChunks == -1)
            expectedChunks = totalChunks;

        byte[] bytes = new byte[reader.Length - 8];
        reader.ReadBytes(bytes);

        receivedChunks[chunkIndex] = bytes;
        Debug.Log($"[FriendVisitManager] Received chunk {chunkIndex + 1}/{totalChunks} ({bytes.Length} bytes)");

        if (receivedChunks.Count >= expectedChunks)
        {
            Debug.Log("[FriendVisitManager] All chunks received, reassembling...");

            List<byte> fullData = new();
            for (int i = 0; i < expectedChunks; i++)
            {
                if (receivedChunks.TryGetValue(i, out var chunk))
                    fullData.AddRange(chunk);
                else
                    Debug.LogWarning($"Missing chunk {i}");
            }

            string dest = Path.Combine(Application.persistentDataPath, spectatorSaveFile);
            File.WriteAllBytes(dest, fullData.ToArray());
            Debug.Log($"[FriendVisitManager] Full save file written ({fullData.Count} bytes) -> {dest}");

            receivedChunks.Clear();
            expectedChunks = -1;

            AutoLoadSpectatorSave(dest);
        }
    }

    private void AutoLoadSpectatorSave(string path)
    {
        try
        {
            SaveManager.Instance.Spectator = true;
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
