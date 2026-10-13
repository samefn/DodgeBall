using System.Collections.Concurrent;

public enum NetworkEventKind
{
    ClientConnected,
    ClientMessage,
    ClientDisconnected,
    HostMessage,
    HostDisconnected
}

public struct NetworkEvent
{
    public NetworkEventKind Kind;
    public int ClientId;
    public string Data;
    public int Generation;
}

// Hand-off point between the socket threads and Unity's main thread.
public class NetworkEventQueue
{
    private readonly ConcurrentQueue<NetworkEvent> events = new ConcurrentQueue<NetworkEvent>();
    private volatile int generation;

    public void Enqueue(NetworkEventKind kind, int clientId, string data)
    {
        events.Enqueue(new NetworkEvent { Kind = kind, ClientId = clientId, Data = data, Generation = generation });
    }

    // Events queued before this call belong to a session that already ended and are dropped.
    public void BeginNewSession()
    {
        generation++;
    }

    public bool TryDequeue(out NetworkEvent networkEvent)
    {
        while (events.TryDequeue(out networkEvent))
        {
            if (networkEvent.Generation == generation)
                return true;
        }
        return false;
    }
}
