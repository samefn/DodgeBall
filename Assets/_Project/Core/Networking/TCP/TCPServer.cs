using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class TCPServer : MonoBehaviour, IServer
{
    [SerializeField] private int maxClients = 5;

    private TcpListener tcpListener;
    private readonly Dictionary<int, TcpConnection> clients = new Dictionary<int, TcpConnection>();
    private readonly object clientsLock = new object();
    private int nextClientId = 1;

    public bool IsServerRunning { get; private set; }
    public int MaxClients => maxClients;

    public int ClientCount
    {
        get { lock (clientsLock) return clients.Count; }
    }

    public event Action<int, string> OnMessageReceived;
    public event Action<int> OnConnected;
    public event Action<int> OnDisconnected;

    public Task StartServer(int port)
    {
        if (IsServerRunning)
        {
            Debug.Log("[Server] Server already running");
            return Task.CompletedTask;
        }

        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
        }
        catch (SocketException e)
        {
            Debug.LogError($"[Server] Could not start on port {port}: {e.Message}");
            tcpListener = null;
            return Task.CompletedTask;
        }

        IsServerRunning = true;
        Debug.Log($"[Server] Server started on port {port}, waiting for connections...");
        _ = AcceptLoop(tcpListener);
        return Task.CompletedTask;
    }

    private async Task AcceptLoop(TcpListener listener)
    {
        while (IsServerRunning && tcpListener == listener)
        {
            TcpClient tcpClient;
            try
            {
                tcpClient = await listener.AcceptTcpClientAsync();
            }
            catch (Exception e)
            {
                if (IsServerRunning)
                    Debug.LogWarning($"[Server] Stopped accepting connections: {e.Message}");
                break;
            }

            try
            {
                HandleNewClient(tcpClient);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Server] Could not accept a client: {e.Message}");
                tcpClient.Close();
            }
        }
    }

    private void HandleNewClient(TcpClient tcpClient)
    {
        if (ClientCount >= maxClients)
        {
            Debug.Log("[Server] Connection rejected: server is full");
            tcpClient.Close();
            return;
        }

        TcpConnection connection;
        lock (clientsLock)
        {
            connection = new TcpConnection(nextClientId++, tcpClient);
            clients.Add(connection.Id, connection);
        }

        connection.MessageReceived += (sender, message) => OnMessageReceived?.Invoke(sender.Id, message);
        connection.Closed += HandleConnectionClosed;

        Debug.Log($"[Server] Client {connection.Id} connected: {connection.RemoteAddress}");
        OnConnected?.Invoke(connection.Id);
        connection.Start();
    }

    private void HandleConnectionClosed(TcpConnection connection)
    {
        lock (clientsLock)
        {
            if (!clients.Remove(connection.Id))
                return;
        }

        Debug.Log($"[Server] Client {connection.Id} disconnected");
        OnDisconnected?.Invoke(connection.Id);
    }

    public Task SendMessageAsync(string message)
    {
        byte[] frame = MessageFraming.Frame(message);
        foreach (TcpConnection connection in SnapshotClients())
            connection.Enqueue(frame);
        return Task.CompletedTask;
    }

    public Task SendMessageToAsync(int clientId, string message)
    {
        TcpConnection connection = GetConnection(clientId);
        connection?.Enqueue(MessageFraming.Frame(message));
        return Task.CompletedTask;
    }

    public string GetClientAddress(int clientId)
    {
        TcpConnection connection = GetConnection(clientId);
        return connection != null ? connection.RemoteAddress : null;
    }

    public void DisconnectClient(int clientId)
    {
        GetConnection(clientId)?.Close();
    }

    public void DisconnectClientAfterFlush(int clientId)
    {
        GetConnection(clientId)?.CloseAfterFlush();
    }

    public void StopServer()
    {
        Stop(true);
    }

    private void Stop(bool graceful)
    {
        if (IsServerRunning)
        {
            IsServerRunning = false;
            tcpListener?.Stop();
            tcpListener = null;
            Debug.Log("[Server] Server stopped");
        }

        foreach (TcpConnection connection in SnapshotClients())
        {
            if (graceful)
                connection.CloseAfterFlush();
            else
                connection.Close();
        }
    }

    private TcpConnection GetConnection(int clientId)
    {
        lock (clientsLock)
            return clients.TryGetValue(clientId, out TcpConnection connection) ? connection : null;
    }

    private List<TcpConnection> SnapshotClients()
    {
        lock (clientsLock)
            return new List<TcpConnection>(clients.Values);
    }

    private void OnDestroy()
    {
        Stop(false);
    }
}
