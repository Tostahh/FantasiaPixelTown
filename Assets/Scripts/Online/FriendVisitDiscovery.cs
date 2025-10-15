using UnityEngine;
using System;
using System.Collections;
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

    private Coroutine broadcastCoroutine;
    private Coroutine searchCoroutine;

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
        StopBroadcast();

        broadcaster = new UdpClient();
        broadcaster.EnableBroadcast = true;
        broadcastData = Encoding.UTF8.GetBytes(friendCode);

        broadcastCoroutine = StartCoroutine(BroadcastLoop());
    }

    private IEnumerator BroadcastLoop()
    {
        while (broadcaster != null && broadcastData != null)
        {
            broadcaster.Send(broadcastData, broadcastData.Length, new IPEndPoint(IPAddress.Broadcast, port));
            yield return new WaitForSecondsRealtime(1f); // use real-time
        }
    }

    public void StopBroadcast()
    {
        if (broadcastCoroutine != null)
        {
            StopCoroutine(broadcastCoroutine);
            broadcastCoroutine = null;
        }

        if (broadcaster != null)
        {
            broadcaster.Close();
            broadcaster = null;
        }
    }

    // -------------------
    // Visitor scanning
    // -------------------
    public void StartListening(Action<string, string> onRoomFound)
    {
        StopListening();

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

        if (listener != null)
            listener.BeginReceive(OnReceived, onRoomFound);
    }

    public void StopListening()
    {
        listener?.Close();
        listener = null;
    }

    public void SearchForRooms(Action<List<RoomInfo>> onRoomsFound, float searchDuration = 3f)
    {
        StopListening();
        List<RoomInfo> foundRooms = new List<RoomInfo>();

        StartListening((code, ip) =>
        {
            if (!foundRooms.Any(r => r.ip == ip))
                foundRooms.Add(new RoomInfo(code, ip));
        });

        if (searchCoroutine != null) StopCoroutine(searchCoroutine);
        searchCoroutine = StartCoroutine(FinishSearchAfterRealtime(searchDuration, foundRooms, onRoomsFound));
    }

    private IEnumerator FinishSearchAfterRealtime(float duration, List<RoomInfo> foundRooms, Action<List<RoomInfo>> callback)
    {
        yield return new WaitForSecondsRealtime(duration);
        StopListening();
        callback?.Invoke(foundRooms);
        searchCoroutine = null;
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
