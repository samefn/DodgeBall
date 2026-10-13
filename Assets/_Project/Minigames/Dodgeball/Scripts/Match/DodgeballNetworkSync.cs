using System.Collections.Generic;
using UnityEngine;

public class DodgeballNetworkSync : MonoBehaviour
{
    [SerializeField] private float snapshotRate = 20f;
    [SerializeField] private DodgeballArenaController arena;
    [SerializeField] private DodgeballMatchController match;
    [SerializeField] private BallSpawner ballSpawner;

    private readonly List<int> staleViewIds = new List<int>();
    private float snapshotTimer;

    private static NetworkService Network => NetworkService.Instance;

    private void Start()
    {
        NetworkClock.Reset();
        if (GameSession.IsOnline && Network != null)
            Network.OnMessage += HandleMessage;
    }

    private void OnDestroy()
    {
        if (Network != null)
            Network.OnMessage -= HandleMessage;
    }

    private void Update()
    {
        if (GameSession.Mode != GameMode.Host || Network == null || !Network.IsHost)
            return;

        snapshotTimer += Time.unscaledDeltaTime;
        if (snapshotTimer < 1f / snapshotRate)
            return;

        snapshotTimer %= 1f / snapshotRate;
        Network.Send(MessageType.WorldState, BuildSnapshot());
    }

    private void HandleMessage(NetworkMessage message)
    {
        bool isInput = message.type == MessageType.PlayerInput || message.type == MessageType.PlayerInputBatch;
        if (GameSession.Mode == GameMode.Host && isInput)
            ApplyRemoteInput(message);
        else if (GameSession.Mode == GameMode.Client && message.type == MessageType.WorldState)
            ApplySnapshot(message.ReadPayload<WorldStatePayload>());
    }

    private void ApplyRemoteInput(NetworkMessage message)
    {
        DodgeballPlayer player = arena.GetPlayer(message.senderId);
        if (player == null || !player.TryGetComponent(out RemoteInputSource remoteInput))
            return;

        if (message.type == MessageType.PlayerInput)
        {
            remoteInput.Receive(message.ReadPayload<PlayerInputState>());
            return;
        }

        InputBatchPayload batch = message.ReadPayload<InputBatchPayload>();
        if (batch.inputs == null)
            return;
        foreach (PlayerInputState input in batch.inputs)
            remoteInput.Receive(input);
    }

    private WorldStatePayload BuildSnapshot()
    {
        List<PlayerStateData> playerStates = new List<PlayerStateData>();
        foreach (DodgeballPlayer player in match.Players)
        {
            playerStates.Add(new PlayerStateData
            {
                playerId = player.PlayerId,
                position = player.transform.position,
                yaw = player.transform.eulerAngles.y,
                pitch = player.Movement.Pitch,
                lives = player.Health.Lives,
                score = match.GetScore(player.PlayerId),
                holdingBall = player.IsHoldingBall,
                crouching = player.IsCrouching,
                grounded = player.IsGrounded,
                throwProgress = player.ThrowProgress,
                throwCharge = player.ThrowCharge,
                lastInputSequence = player.LastInputSequence
            });
        }

        List<BallStateData> ballStates = new List<BallStateData>();
        foreach (DodgeBall ball in ballSpawner.Balls)
        {
            ballStates.Add(new BallStateData
            {
                ballId = ball.BallId,
                position = ball.transform.position,
                state = (int)ball.State,
                ownerId = ball.OwnerId
            });
        }

        return new WorldStatePayload
        {
            serverTime = NetworkClock.HostTime,
            phase = (int)match.Phase,
            timeRemaining = match.TimeRemaining,
            countdown = match.Countdown,
            winnerId = match.WinnerId,
            players = playerStates.ToArray(),
            balls = ballStates.ToArray()
        };
    }

    private void ApplySnapshot(WorldStatePayload snapshot)
    {
        if (snapshot.players == null || snapshot.balls == null)
            return;

        NetworkClock.OnSnapshot(snapshot.serverTime);
        MatchPhase phase = (MatchPhase)snapshot.phase;

        staleViewIds.Clear();
        staleViewIds.AddRange(arena.PlayerIds);

        foreach (PlayerStateData state in snapshot.players)
        {
            DodgeballPlayer player = arena.GetOrCreateView(state.playerId);
            player.ApplyNetworkState(state);
            player.Health.SetLivesFromNetwork(state.lives);
            player.SetMatchActive(phase == MatchPhase.Playing);
            player.GetComponent<NetworkPlayerView>().ApplyState(state, snapshot.serverTime);
            match.SetNetworkScore(state.playerId, state.score);
            staleViewIds.Remove(state.playerId);
        }

        foreach (int playerId in staleViewIds)
            arena.RemovePlayer(playerId);

        foreach (BallStateData ballState in snapshot.balls)
        {
            DodgeBall ball = ballSpawner.GetBall(ballState.ballId);
            if (ball == null)
                continue;

            DodgeballPlayer holder = arena.GetPlayer(ballState.ownerId);
            Transform hand = (BallState)ballState.state == BallState.Held && holder != null ? holder.Balls.HoldPoint : null;
            ball.ApplyNetworkState(ballState.position, (BallState)ballState.state, ballState.ownerId, hand, snapshot.serverTime);
        }

        match.ApplyNetworkState(phase, snapshot.timeRemaining, snapshot.countdown, snapshot.winnerId);
    }
}
