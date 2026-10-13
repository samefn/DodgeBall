using System;
using System.Threading.Tasks;

public interface IServer
{
    bool IsServerRunning { get; }

    event Action<int, string> OnMessageReceived;
    event Action<int> OnConnected;
    event Action<int> OnDisconnected;

    Task StartServer(int port);
    Task SendMessageAsync(string message);
    Task SendMessageToAsync(int clientId, string message);
    string GetClientAddress(int clientId);
    void DisconnectClient(int clientId);
    void DisconnectClientAfterFlush(int clientId);
    void StopServer();
}
