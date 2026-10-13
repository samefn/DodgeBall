using System;
using System.Threading.Tasks;

public interface IClient
{
    bool IsConnected { get; }

    event Action<string> OnMessageReceived;
    event Action OnDisconnected;

    Task ConnectToServer(string ip, int port);
    Task SendMessageAsync(string message);
    void DisconnectAfterFlush();
    void Disconnect();
}
