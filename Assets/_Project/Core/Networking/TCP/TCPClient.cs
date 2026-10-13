using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class TCPClient : MonoBehaviour, IClient
{
    [Tooltip("Seconds before a connection attempt is abandoned.")]
    [SerializeField] private float connectTimeout = 5f;

    private volatile TcpConnection connection;

    public bool IsConnected => connection != null && connection.IsOpen;

    public event Action<string> OnMessageReceived;
    public event Action OnDisconnected;

    public async Task ConnectToServer(string ip, int port)
    {
        if (IsConnected)
        {
            Debug.Log("[Client] Already connected");
            return;
        }

        TcpClient tcpClient = new TcpClient();
        try
        {
            Task connectTask = tcpClient.ConnectAsync(ip, port);
            Task finished = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(connectTimeout)));
            if (finished != connectTask)
                throw new TimeoutException("connection timed out");

            await connectTask;
        }
        catch (Exception e) when (e is SocketException || e is ArgumentException || e is TimeoutException || e is ObjectDisposedException)
        {
            Debug.LogWarning($"[Client] Could not connect to {ip}:{port}: {e.Message}");
            tcpClient.Close();
            return;
        }

        connection = new TcpConnection(0, tcpClient);
        connection.MessageReceived += (sender, message) => OnMessageReceived?.Invoke(message);
        connection.Closed += HandleClosed;

        Debug.Log("[Client] Connected to server");
        connection.Start();
    }

    public Task SendMessageAsync(string message)
    {
        if (!IsConnected)
            return Task.CompletedTask;

        connection.Enqueue(MessageFraming.Frame(message));
        return Task.CompletedTask;
    }

    public void DisconnectAfterFlush()
    {
        connection?.CloseAfterFlush();
    }

    public void Disconnect()
    {
        connection?.Close();
    }

    private void HandleClosed(TcpConnection closedConnection)
    {
        if (closedConnection != connection)
            return;

        connection = null;
        Debug.Log("[Client] Disconnected");
        OnDisconnected?.Invoke();
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}
