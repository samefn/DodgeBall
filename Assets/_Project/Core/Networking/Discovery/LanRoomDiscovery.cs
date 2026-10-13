using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LanRoomDiscovery : MonoBehaviour
{
    public const int DiscoveryPort = 47777;

    private const string GameId = "DodgeballFPS";
    private const float BeaconInterval = 1f;
    private const float RoomTimeout = 3.5f;

    private readonly Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> expiredRooms = new List<string>();
    private UdpClient sender;
    private UdpClient listener;
    private RoomBeacon beacon;
    private float beaconTimer;

    public IReadOnlyCollection<RoomInfo> Rooms => rooms.Values;

    public event Action OnRoomsChanged;

    public void StartAdvertising(string roomName, int port, int maxPlayers)
    {
        StopAdvertising();
        beacon = new RoomBeacon
        {
            game = GameId,
            roomName = roomName,
            port = port,
            playerCount = 1,
            maxPlayers = maxPlayers
        };
        sender = new UdpClient { EnableBroadcast = true };
        beaconTimer = BeaconInterval;
    }

    public void SetPlayerCount(int playerCount)
    {
        if (beacon != null)
            beacon.playerCount = playerCount;
    }

    public void StopAdvertising()
    {
        sender?.Close();
        sender = null;
        beacon = null;
    }

    public void StartListening()
    {
        if (listener != null)
            return;

        UdpClient client = new UdpClient();
        try
        {
            client.ExclusiveAddressUse = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
        }
        catch (SocketException e)
        {
            Debug.LogWarning($"[Discovery] Could not listen on UDP port {DiscoveryPort}: {e.Message}");
            client.Close();
            return;
        }

        listener = client;
        _ = ReceiveLoop(client);
    }

    public void StopListening()
    {
        listener?.Close();
        listener = null;

        if (rooms.Count == 0)
            return;

        rooms.Clear();
        OnRoomsChanged?.Invoke();
    }

    public bool TryGetRoom(string roomName, out RoomInfo room)
    {
        return rooms.TryGetValue(roomName.Trim(), out room);
    }

    private async Task ReceiveLoop(UdpClient client)
    {
        while (listener == client)
        {
            UdpReceiveResult result;
            try
            {
                result = await client.ReceiveAsync();
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                continue;
            }

            HandleBeacon(result);
        }
    }

    private void HandleBeacon(UdpReceiveResult result)
    {
        RoomBeacon received;
        try
        {
            received = JsonUtility.FromJson<RoomBeacon>(Encoding.UTF8.GetString(result.Buffer));
        }
        catch (ArgumentException)
        {
            return;
        }

        if (received == null || received.game != GameId || string.IsNullOrWhiteSpace(received.roomName))
            return;

        rooms[received.roomName] = new RoomInfo(
            received.roomName,
            result.RemoteEndPoint.Address.ToString(),
            received.port,
            received.playerCount,
            received.maxPlayers,
            Time.realtimeSinceStartup);

        OnRoomsChanged?.Invoke();
    }

    private void Update()
    {
        if (sender != null)
        {
            beaconTimer += Time.unscaledDeltaTime;
            if (beaconTimer >= BeaconInterval)
            {
                beaconTimer = 0f;
                SendBeacon();
            }
        }

        RemoveExpiredRooms();
    }

    private void SendBeacon()
    {
        byte[] data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(beacon));
        Send(data, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
        foreach (IPAddress subnetBroadcast in GetSubnetBroadcasts())
            Send(data, new IPEndPoint(subnetBroadcast, DiscoveryPort));
        Send(data, new IPEndPoint(IPAddress.Loopback, DiscoveryPort));
    }

    private const float AdapterRefreshInterval = 10f;
    private readonly List<IPAddress> subnetBroadcasts = new List<IPAddress>();
    private float nextAdapterRefresh;

    private List<IPAddress> GetSubnetBroadcasts()
    {
        if (Time.realtimeSinceStartup < nextAdapterRefresh)
            return subnetBroadcasts;

        nextAdapterRefresh = Time.realtimeSinceStartup + AdapterRefreshInterval;
        subnetBroadcasts.Clear();
        foreach (UnicastIPAddressInformation address in GetLanAddresses())
        {
            if (address.IPv4Mask == null)
                continue;

            byte[] ip = address.Address.GetAddressBytes();
            byte[] mask = address.IPv4Mask.GetAddressBytes();
            if (ip.Length != 4 || mask.Length != 4)
                continue;

            byte[] broadcast = new byte[4];
            for (int i = 0; i < 4; i++)
                broadcast[i] = (byte)(ip[i] | ~mask[i]);
            subnetBroadcasts.Add(new IPAddress(broadcast));
        }
        return subnetBroadcasts;
    }

    public static List<string> GetLocalIPv4Addresses()
    {
        List<string> result = new List<string>();
        foreach (UnicastIPAddressInformation address in GetLanAddresses())
            result.Add(address.Address.ToString());
        return result;
    }

    private static IEnumerable<UnicastIPAddressInformation> GetLanAddresses()
    {
        NetworkInterface[] adapters;
        try
        {
            adapters = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch (NetworkInformationException)
        {
            yield break;
        }

        foreach (NetworkInterface adapter in adapters)
        {
            if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address.Address))
                    continue;
                if (address.Address.GetAddressBytes()[0] == 169 && address.Address.GetAddressBytes()[1] == 254)
                    continue;
                yield return address;
            }
        }
    }

    private void Send(byte[] data, IPEndPoint endPoint)
    {
        try
        {
            sender.Send(data, data.Length, endPoint);
        }
        catch (SocketException e)
        {
            Debug.LogWarning($"[Discovery] Could not send beacon to {endPoint}: {e.Message}");
        }
    }

    private void RemoveExpiredRooms()
    {
        expiredRooms.Clear();
        foreach (RoomInfo room in rooms.Values)
        {
            if (Time.realtimeSinceStartup - room.LastSeen > RoomTimeout)
                expiredRooms.Add(room.RoomName);
        }

        if (expiredRooms.Count == 0)
            return;

        foreach (string roomName in expiredRooms)
            rooms.Remove(roomName);

        OnRoomsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        StopAdvertising();
        StopListening();
    }
}
