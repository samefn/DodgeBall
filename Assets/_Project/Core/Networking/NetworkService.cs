using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class NetworkService : MonoBehaviour
{
    public const int HostPlayerId = 1;
    private const float PingInterval = 1f;
    private const float ConnectionTimeout = 20f;
    private const int MaxEventsPerFrame = 2000;

    private static readonly HashSet<MessageType> AllowedFromClients = new HashSet<MessageType>
    {
        MessageType.Connect,
        MessageType.Disconnect,
        MessageType.Ping,
        MessageType.Pong,
        MessageType.PlayerInput,
        MessageType.PlayerInputBatch
    };

    public static NetworkService Instance { get; private set; }

    private TCPServer tcpServer;

    private IServer server;
    private IClient client;
    private readonly NetworkEventQueue inbound = new NetworkEventQueue();
    private readonly PingTable pings = new PingTable();
    private readonly ConnectionTimeouts timeouts = new ConnectionTimeouts(ConnectionTimeout);
    private readonly List<int> remotePlayerIds = new List<int>();
    private readonly List<int> lobbyPlayerIds = new List<int>();
    private readonly HashSet<int> closingClients = new HashSet<int>();
    private int sequence;
    private float pingTimer;
    private float lastHeardFromHost;
    private bool matchInProgress;

    public bool IsHost { get; private set; }
    public bool IsClient { get; private set; }
    public bool IsActive => IsHost || IsClient;
    public int LocalPlayerId { get; private set; }
    public int Port { get; private set; }
    public string RoomName { get; private set; }
    public bool HostMatchInProgress { get; private set; }
    public LanRoomDiscovery Discovery { get; private set; }
    public IReadOnlyList<int> RemotePlayerIds => remotePlayerIds;
    public IReadOnlyList<int> LobbyPlayerIds => lobbyPlayerIds;

    public event Action<NetworkMessage> OnMessage;
    public event Action<int> OnPlayerJoined;
    public event Action<int> OnPlayerLeft;
    public event Action OnLobbyChanged;
    public event Action OnDisconnectedFromHost;
    public event Action OnRulesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateDefaultInstance()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject(nameof(NetworkService));
        root.AddComponent<TCPServer>();
        root.AddComponent<TCPClient>();
        root.AddComponent<LanRoomDiscovery>();
        root.AddComponent<NetworkService>();
        root.AddComponent<HostAdminPanel>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;

        tcpServer = GetComponent<TCPServer>();
        server = tcpServer;
        client = GetComponent<TCPClient>();
        Discovery = GetComponent<LanRoomDiscovery>();

        BanList.IsBanned(string.Empty);

        server.OnConnected += id => inbound.Enqueue(NetworkEventKind.ClientConnected, id, null);
        server.OnMessageReceived += (id, raw) => inbound.Enqueue(NetworkEventKind.ClientMessage, id, raw);
        server.OnDisconnected += id => inbound.Enqueue(NetworkEventKind.ClientDisconnected, id, null);
        client.OnMessageReceived += raw => inbound.Enqueue(NetworkEventKind.HostMessage, 0, raw);
        client.OnDisconnected += () => inbound.Enqueue(NetworkEventKind.HostDisconnected, 0, null);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        ProcessInbound();

        if (IsHost)
            UpdateHost();
        else if (IsClient)
            UpdateClient();
    }

    private void ProcessInbound()
    {
        for (int i = 0; i < MaxEventsPerFrame && inbound.TryDequeue(out NetworkEvent networkEvent); i++)
        {
            try
            {
                Dispatch(networkEvent);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void Dispatch(NetworkEvent networkEvent)
    {
        switch (networkEvent.Kind)
        {
            case NetworkEventKind.ClientConnected:
                HandleClientConnected(networkEvent.ClientId);
                break;
            case NetworkEventKind.ClientMessage:
                HandleServerMessage(networkEvent.ClientId, networkEvent.Data);
                break;
            case NetworkEventKind.ClientDisconnected:
                HandleClientDisconnected(networkEvent.ClientId);
                break;
            case NetworkEventKind.HostMessage:
                HandleClientMessage(networkEvent.Data);
                break;
            case NetworkEventKind.HostDisconnected:
                HandleDisconnectedFromHost();
                break;
        }
    }

    public async Task<bool> Host(int port, string roomName = null)
    {
        if (IsActive)
            return false;

        inbound.BeginNewSession();
        await server.StartServer(port);
        if (!server.IsServerRunning)
            return false;

        IsHost = true;
        Port = port;
        RoomName = roomName;
        LocalPlayerId = HostPlayerId;
        pingTimer = 0f;

        if (Discovery != null && !string.IsNullOrEmpty(roomName))
            Discovery.StartAdvertising(roomName, port, tcpServer.MaxClients + 1);

        RefreshLobby();
        return true;
    }

    public async Task<bool> Join(string ip, int port, string roomName = null)
    {
        if (IsActive)
            return false;

        if (client.IsConnected)
            client.Disconnect();

        inbound.BeginNewSession();
        await client.ConnectToServer(ip, port);
        if (!client.IsConnected)
            return false;

        IsClient = true;
        Port = port;
        RoomName = roomName;
        LocalPlayerId = 0;
        pingTimer = 0f;
        lastHeardFromHost = Time.realtimeSinceStartup;
        GameSession.LastDisconnectReason = null;
        Send(MessageType.Connect);
        return true;
    }

    public void Shutdown()
    {
        bool wasHost = IsHost;
        bool wasClient = IsClient;

        if (wasClient)
            Send(MessageType.Disconnect);
        if (wasHost)
            Send(MessageType.Kick, new KickPayload { reason = "El host cerró la sala." });

        ResetState();

        if (wasHost)
        {
            if (Discovery != null)
                Discovery.StopAdvertising();
            server.StopServer();
        }
        if (wasClient)
            client.DisconnectAfterFlush();
    }

    public int GetPing(int playerId)
    {
        if (IsHost)
            return playerId == HostPlayerId ? 0 : pings.GetMeasured(playerId);

        if (playerId == LocalPlayerId)
            return Mathf.RoundToInt(pings.LocalRoundTripMs);
        return pings.GetReported(playerId);
    }

    public void Send(MessageType type)
    {
        SendToAll(BuildMessage(type, string.Empty));
    }

    public void Send<T>(MessageType type, T payload)
    {
        SendToAll(BuildMessage(type, JsonUtility.ToJson(payload)));
    }

    public void SendTo(int playerId, MessageType type)
    {
        SendToPlayer(playerId, BuildMessage(type, string.Empty));
    }

    public void SendTo<T>(int playerId, MessageType type, T payload)
    {
        SendToPlayer(playerId, BuildMessage(type, JsonUtility.ToJson(payload)));
    }

    private string BuildMessage(MessageType type, string payload)
    {
        return new NetworkMessage
        {
            type = type,
            senderId = LocalPlayerId,
            sequence = ++sequence,
            payload = payload
        }.Serialize();
    }

    private void SendToAll(string json)
    {
        if (IsHost)
            _ = server.SendMessageAsync(json);
        else if (IsClient)
            _ = client.SendMessageAsync(json);
        else
            return;

    }

    private void SendToPlayer(int playerId, string json)
    {
        if (!IsHost)
            return;

        _ = server.SendMessageToAsync(ToClientId(playerId), json);
    }

    private void ResetState()
    {
        inbound.BeginNewSession();
        IsHost = false;
        IsClient = false;
        LocalPlayerId = 0;
        RoomName = null;
        matchInProgress = false;
        HostMatchInProgress = false;
        remotePlayerIds.Clear();
        lobbyPlayerIds.Clear();
        timeouts.Clear();
        pings.Clear();
        closingClients.Clear();
    }
}
