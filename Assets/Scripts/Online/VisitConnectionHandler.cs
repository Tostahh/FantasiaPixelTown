using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class VisitConnectionHandler
{
    private readonly NetworkDriver driver;
    private NetworkConnection connection;
    private readonly Action<DataStreamReader> onDataReceived;
    private readonly Action onConnected;
    private readonly Action onDisconnected;

    public VisitConnectionHandler(
        NetworkDriver driver,
        NetworkConnection connection,
        Action<DataStreamReader> onDataReceived,
        Action onConnected = null,
        Action onDisconnected = null)
    {
        this.driver = driver;
        this.connection = connection;
        this.onDataReceived = onDataReceived;
        this.onConnected = onConnected;
        this.onDisconnected = onDisconnected;
    }

    public void PollEvents()
    {
        if (!connection.IsCreated) return;

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            switch (cmd)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("[VisitConnectionHandler] Connected to host.");
                    onConnected?.Invoke();
                    break;

                case NetworkEvent.Type.Data:
                    // Forward all received data to FriendVisitManager for processing
                    onDataReceived?.Invoke(stream);
                    break;

                case NetworkEvent.Type.Disconnect:
                    Debug.LogWarning("[VisitConnectionHandler] Disconnected from host.");
                    connection = default;
                    onDisconnected?.Invoke();
                    break;
            }
        }
    }

    public bool IsConnected => connection.IsCreated;

    public void Disconnect()
    {
        if (connection.IsCreated)
        {
            connection.Disconnect(driver);
            connection = default;
        }
    }
}
