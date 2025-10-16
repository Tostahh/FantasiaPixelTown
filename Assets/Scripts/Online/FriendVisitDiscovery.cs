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
    private const int port = 2468;
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

        try
        {
            broadcaster = new UdpClient();
            broadcaster.EnableBroadcast = true;
            broadcaster.MulticastLoopback = true; // allows host to see its own broadcast
            broadcastData = Encoding.UTF8.GetBytes(friendCode);

            Debug.Log("[FriendVisitDiscovery] Starting broadcast...");
            broadcastCoroutine = StartCoroutine(BroadcastLoop());
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendVisitDiscovery] Failed to start broadcast: {ex.Message}");
        }
    }

    private IEnumerator BroadcastLoop()
    {
        IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, port);
        Debug.Log($"[FriendVisitDiscovery] Using broadcast {broadcastEP}");

        while (broadcaster != null && broadcastData != null)
        {
            try
            {
                broadcaster.Send(broadcastData, broadcastData.Length, broadcastEP);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FriendVisitDiscovery] Broadcast error: {ex.Message}");
            }

            yield return new WaitForSecondsRealtime(1f);
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
        StopListening(); // close any existing listener

        try
        {
            listener = new UdpClient(new IPEndPoint(IPAddress.Any, port));

            // Allow multiple listeners on same port for same-machine testing
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.EnableBroadcast = true;

            Debug.Log($"[FriendVisitDiscovery] Listening on UDP port {port}");
            listener.BeginReceive(OnReceived, onRoomFound);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendVisitDiscovery] Failed to start listener: {ex.Message}");
        }
    }

    private void OnReceived(IAsyncResult ar)
    {
        if (listener == null) return;

        var onRoomFound = (Action<string, string>)ar.AsyncState;
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

        try
        {
            byte[] data = listener.EndReceive(ar, ref ep);
            string receivedCode = Encoding.UTF8.GetString(data);

            Debug.Log($"[FriendVisitDiscovery] Received broadcast '{receivedCode}' from {ep.Address}");
            onRoomFound?.Invoke(receivedCode, ep.Address.ToString());
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendVisitDiscovery] Receive error: {ex.Message}");
        }

        // Continue listening safely
        try
        {
            listener?.BeginReceive(OnReceived, onRoomFound);
        }
        catch { }
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

        Debug.Log("[FriendVisitDiscovery] Starting search for rooms...");

        StartListening((code, ip) =>
        {
            Debug.Log($"[FriendVisitDiscovery] Room found! {code} @ {ip}");
            if (!foundRooms.Any(r => r.ip == ip))
                foundRooms.Add(new RoomInfo(code, ip));
        });

        if (searchCoroutine != null)
            StopCoroutine(searchCoroutine);

        searchCoroutine = StartCoroutine(FinishSearchAfterRealtime(searchDuration, foundRooms, onRoomsFound));
    }

    private IEnumerator FinishSearchAfterRealtime(float duration, List<RoomInfo> foundRooms, Action<List<RoomInfo>> callback)
    {
        yield return new WaitForSecondsRealtime(duration);
        StopListening();
        Debug.Log($"[FriendVisitDiscovery] Search finished. Found {foundRooms.Count} rooms.");
        callback?.Invoke(foundRooms);
        searchCoroutine = null;
    }

    private void OnApplicationQuit()
    {
        StopListening();
        StopBroadcast();
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
