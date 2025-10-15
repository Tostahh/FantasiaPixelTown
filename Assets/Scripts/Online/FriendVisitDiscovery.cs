using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

public class FriendVisitDiscovery : MonoBehaviour
{
    public static FriendVisitDiscovery Instance;

    private UdpClient listener;
    private UdpClient broadcaster;
    private const int port = 7778; // separate discovery port
    private byte[] broadcastData;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------
    // Host broadcasting
    // -------------------
    public void StartBroadcast(string friendCode)
    {
        if (broadcaster != null) StopBroadcast();

        broadcaster = new UdpClient();
        broadcaster.EnableBroadcast = true;
        broadcastData = Encoding.UTF8.GetBytes(friendCode);

        InvokeRepeating(nameof(Broadcast), 0f, 1f); // now calls class-level method
    }

    private void Broadcast()
    {
        if (broadcaster != null && broadcastData != null)
        {
            broadcaster.Send(broadcastData, broadcastData.Length, new IPEndPoint(IPAddress.Broadcast, port));
        }
    }

    public void StopBroadcast()
    {
        if (broadcaster != null)
        {
            CancelInvoke(nameof(Broadcast));
            broadcaster.Close();
            broadcaster = null;
        }
    }

    // -------------------
    // Visitor scanning
    // -------------------
    public void StartListening(Action<string, string> onRoomFound)
    {
        if (listener != null) StopListening();

        listener = new UdpClient(port);
        listener.BeginReceive(OnReceived, onRoomFound);
    }

    private void OnReceived(IAsyncResult ar)
    {
        var onRoomFound = (Action<string, string>)ar.AsyncState;
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        byte[] data = listener.EndReceive(ar, ref ep);
        string receivedCode = Encoding.UTF8.GetString(data);

        onRoomFound?.Invoke(receivedCode, ep.Address.ToString());

        listener.BeginReceive(OnReceived, onRoomFound);
    }

    public void StopListening()
    {
        listener?.Close();
        listener = null;
    }

    public void SearchForRooms(Action<List<RoomInfo>> onRoomsFound, float searchDuration = 3f)
    {
        List<RoomInfo> foundRooms = new List<RoomInfo>();

        // Start listening
        StartListening((code, ip) =>
        {
            // Avoid duplicates
            if (!foundRooms.Any(r => r.ip == ip))
            {
                foundRooms.Add(new RoomInfo(code, ip));
            }
        });

        Invoke(nameof(FinishSearch), searchDuration);

        void FinishSearch()
        {
            StopListening();
            onRoomsFound?.Invoke(foundRooms);
        }
    }

    public class RoomInfo
    {
        public string friendCode;
        public string ip;

        public RoomInfo(string code, string ip)
        {
            friendCode = code;
            this.ip = ip;
        }
    }

    private void OnApplicationQuit()
    {
        StopListening();
        StopBroadcast();
    }
}
