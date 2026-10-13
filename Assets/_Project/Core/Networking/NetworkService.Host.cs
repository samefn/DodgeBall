using UnityEngine;

public partial class NetworkService
{
    private void UpdateHost()
    {
        float now = Time.realtimeSinceStartup;

        foreach (int clientId in timeouts.CollectExpired(now))
        {
            Debug.LogWarning($"[Network] Player {ToPlayerId(clientId)} timed out");
            server.DisconnectClient(clientId);
        }

        pingTimer += Time.unscaledDeltaTime;
        if (pingTimer < PingInterval)
            return;

        pingTimer = 0f;
        foreach (int playerId in remotePlayerIds)
            SendTo(playerId, MessageType.Ping, new PingPayload { sentAt = now });

        RefreshLobby();
    }

    public void SetMatchInProgress(bool inProgress)
    {
        if (!IsHost || matchInProgress == inProgress)
            return;

        matchInProgress = inProgress;
        RefreshLobby();
    }

    public void Kick(int playerId, string reason)
    {
        if (!IsHost || !remotePlayerIds.Contains(playerId))
            return;

        int clientId = ToClientId(playerId);
        SendTo(playerId, MessageType.Kick, new KickPayload { reason = reason });
        closingClients.Add(clientId);
        timeouts.Forget(clientId);
        server.DisconnectClientAfterFlush(clientId);

        RemoveRemotePlayer(playerId);
    }

    public void Ban(int playerId)
    {
        string address = GetPlayerAddress(playerId);
        if (string.IsNullOrEmpty(address))
            return;

        BanList.Ban(address);
        Kick(playerId, "Has sido baneado de esta sala.");
    }

    public string GetPlayerAddress(int playerId)
    {
        return IsHost ? server.GetClientAddress(ToClientId(playerId)) : null;
    }

    public void BroadcastRules()
    {
        if (!IsHost)
            return;

        Send(MessageType.RulesUpdate, GameSession.Rules);
        OnRulesChanged?.Invoke();
    }

    private void HandleClientConnected(int clientId)
    {
        if (!IsHost)
            return;

        if (BanList.IsBanned(server.GetClientAddress(clientId)))
        {
            Debug.Log($"[Network] Rejected banned address {server.GetClientAddress(clientId)}");
            closingClients.Add(clientId);
            SendToPlayer(ToPlayerId(clientId), BuildMessage(MessageType.Kick, JsonUtility.ToJson(new KickPayload { reason = "Estás baneado de esta sala." })));
            server.DisconnectClientAfterFlush(clientId);
            return;
        }

        timeouts.MarkHeard(clientId, Time.realtimeSinceStartup);
    }

    private void HandleServerMessage(int clientId, string raw)
    {
        if (!IsHost || closingClients.Contains(clientId))
            return;

        timeouts.MarkHeard(clientId, Time.realtimeSinceStartup);
        int playerId = ToPlayerId(clientId);

        if (!NetworkMessage.TryParse(raw, out NetworkMessage message) || !AllowedFromClients.Contains(message.type))
        {
            Debug.LogWarning($"[Network] Rejected message from player {playerId}");
            return;
        }

        message.senderId = playerId;

        switch (message.type)
        {
            case MessageType.Connect:
                AcceptPlayer(playerId);
                return;
            case MessageType.Ping:
                SendToPlayer(playerId, BuildMessage(MessageType.Pong, message.payload));
                return;
            case MessageType.Pong:
                pings.StoreRoundTrip(playerId, message.ReadPayload<PingPayload>().sentAt, Time.realtimeSinceStartup);
                return;
            case MessageType.Disconnect:
                closingClients.Add(clientId);
                timeouts.Forget(clientId);
                server.DisconnectClient(clientId);
                RemoveRemotePlayer(playerId);
                return;
        }

        if (remotePlayerIds.Contains(playerId))
            OnMessage?.Invoke(message);
    }

    private void AcceptPlayer(int playerId)
    {
        if (remotePlayerIds.Contains(playerId))
            return;

        remotePlayerIds.Add(playerId);
        SendTo(playerId, MessageType.ConnectAccept, new ConnectAcceptPayload { playerId = playerId });
        SendTo(playerId, MessageType.RulesUpdate, GameSession.Rules);
        RefreshLobby();
        Debug.Log($"[Network] Player {playerId} joined");
        OnPlayerJoined?.Invoke(playerId);
    }

    private void HandleClientDisconnected(int clientId)
    {
        closingClients.Remove(clientId);
        timeouts.Forget(clientId);
        if (IsHost)
            RemoveRemotePlayer(ToPlayerId(clientId));
    }

    private void RemoveRemotePlayer(int playerId)
    {
        if (!remotePlayerIds.Remove(playerId))
            return;

        pings.Remove(playerId);
        RefreshLobby();
        Debug.Log($"[Network] Player {playerId} left");
        OnPlayerLeft?.Invoke(playerId);
    }

    private void RefreshLobby()
    {
        lobbyPlayerIds.Clear();
        lobbyPlayerIds.Add(HostPlayerId);
        lobbyPlayerIds.AddRange(remotePlayerIds);

        int[] playerPings = new int[lobbyPlayerIds.Count];
        for (int i = 0; i < playerPings.Length; i++)
            playerPings[i] = GetPing(lobbyPlayerIds[i]);

        Send(MessageType.LobbyUpdate, new LobbyPayload
        {
            playerIds = lobbyPlayerIds.ToArray(),
            pings = playerPings,
            matchInProgress = matchInProgress
        });
        HostMatchInProgress = matchInProgress;

        if (Discovery != null)
            Discovery.SetPlayerCount(lobbyPlayerIds.Count);
        OnLobbyChanged?.Invoke();
    }

    private static int ToPlayerId(int clientId) => clientId + HostPlayerId;
    private static int ToClientId(int playerId) => playerId - HostPlayerId;
}
