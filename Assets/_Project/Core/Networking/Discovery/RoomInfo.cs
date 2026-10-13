public class RoomInfo
{
    public string RoomName { get; }
    public string Address { get; }
    public int Port { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }
    public float LastSeen { get; }

    public bool IsFull => PlayerCount >= MaxPlayers;

    public RoomInfo(string roomName, string address, int port, int playerCount, int maxPlayers, float lastSeen)
    {
        RoomName = roomName;
        Address = address;
        Port = port;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
        LastSeen = lastSeen;
    }
}
