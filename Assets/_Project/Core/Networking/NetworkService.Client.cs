using UnityEngine;

public partial class NetworkService
{
    private void UpdateClient()
    {
        if (Time.realtimeSinceStartup - lastHeardFromHost > ConnectionTimeout)
        {
            Debug.LogWarning("[Network] The host stopped responding");
            GameSession.LastDisconnectReason = "El host dejó de responder.";
            client.Disconnect();
            return;
        }

        if (LocalPlayerId == 0)
            return;

        pingTimer += Time.unscaledDeltaTime;
        if (pingTimer < PingInterval)
            return;

        pingTimer = 0f;
        Send(MessageType.Ping, new PingPayload { sentAt = Time.realtimeSinceStartup });
    }

    private void HandleClientMessage(string raw)
    {
        if (!IsClient)
            return;

        lastHeardFromHost = Time.realtimeSinceStartup;

        if (!NetworkMessage.TryParse(raw, out NetworkMessage message))
        {
            Debug.LogWarning("[Network] Invalid message from host");
            return;
        }

        switch (message.type)
        {
            case MessageType.ConnectAccept:
                LocalPlayerId = message.ReadPayload<ConnectAcceptPayload>().playerId;
                OnLobbyChanged?.Invoke();
                return;
            case MessageType.LobbyUpdate:
                ApplyLobby(message.ReadPayload<LobbyPayload>());
                return;
            case MessageType.Ping:
                Send(MessageType.Pong, message.ReadPayload<PingPayload>());
                return;
            case MessageType.Pong:
                pings.StoreLocalRoundTrip(message.ReadPayload<PingPayload>().sentAt, Time.realtimeSinceStartup);
                return;
            case MessageType.Kick:
                GameSession.LastDisconnectReason = message.ReadPayload<KickPayload>().reason;
                return;
            case MessageType.RulesUpdate:
                ApplyRules(message.ReadPayload<MatchRules>());
                return;
        }

        OnMessage?.Invoke(message);
    }

    private void ApplyLobby(LobbyPayload lobby)
    {
        lobbyPlayerIds.Clear();
        if (lobby.playerIds != null)
            lobbyPlayerIds.AddRange(lobby.playerIds);
        pings.SetReported(lobby.playerIds, lobby.pings);

        HostMatchInProgress = lobby.matchInProgress;
        OnLobbyChanged?.Invoke();
    }

    private void HandleDisconnectedFromHost()
    {
        if (!IsClient)
            return;

        if (string.IsNullOrEmpty(GameSession.LastDisconnectReason))
            GameSession.LastDisconnectReason = "Se perdió la conexión con el host.";

        ResetState();
        OnDisconnectedFromHost?.Invoke();
    }

    private void ApplyRules(MatchRules rules)
    {
        if (rules == null)
            return;

        rules.Clamp();
        GameSession.Rules = rules;
        OnRulesChanged?.Invoke();
    }
}
