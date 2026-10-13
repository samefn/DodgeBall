using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DodgeballMatchController : MonoBehaviour, IMinigame
{
    public const int NoWinner = -1;
    public const string Objective =
        "Golpea a tus rivales con los balones para quitarles vidas. " +
        "Atrapa balones en el aire para sumar puntos. " +
        "Gana el último jugador en pie o, si se acaba el tiempo, el que tenga más puntos.";

    [SerializeField] private float matchDuration = 180f;
    [SerializeField] private int pointsPerHit = 1;
    [SerializeField] private int pointsPerCatch = 1;
    [SerializeField] private int pointsPerElimination = 2;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] private Transform[] spawnPoints;

    private readonly List<DodgeballPlayer> players = new List<DodgeballPlayer>();
    private readonly Dictionary<int, int> scores = new Dictionary<int, int>();
    private MinigameContext context;
    private MatchRules activeRules;

    public MatchPhase Phase { get; private set; } = MatchPhase.Waiting;
    public float TimeRemaining { get; private set; }
    public float Countdown { get; private set; }
    public int WinnerId { get; private set; } = NoWinner;
    public IReadOnlyList<DodgeballPlayer> Players => players;

    private bool IsAuthoritative => context == null || context.IsAuthoritative;

    public event Action OnMatchStarted;
    public event Action<int> OnMatchEnded;

    public void Initialize(MinigameContext minigameContext)
    {
        context = minigameContext;
    }

    public void RegisterPlayer(DodgeballPlayer player)
    {
        players.Add(player);
        scores[player.PlayerId] = 0;

        if (!IsAuthoritative)
            return;

        player.Health.OnHit += HandlePlayerHit;
        player.Balls.OnBallCaught += HandleBallCaught;

        if (activeRules != null)
        {
            player.Health.ResetLives(activeRules.lives);
            player.Balls.ThrowPowerMultiplier = activeRules.throwPowerMultiplier;
        }
        player.SetMatchActive(Phase == MatchPhase.Playing);
    }

    public void UnregisterPlayer(int playerId)
    {
        DodgeballPlayer player = players.Find(candidate => candidate.PlayerId == playerId);
        if (player == null)
            return;

        player.Health.OnHit -= HandlePlayerHit;
        player.Balls.OnBallCaught -= HandleBallCaught;
        players.Remove(player);
        scores.Remove(playerId);

        if (IsAuthoritative && Phase == MatchPhase.Playing)
            CheckForWinner();
    }

    public Vector3 GetSpawnPosition(int index, IEnumerable<Vector3> otherPlayers)
    {
        ArenaBounds bounds = ArenaBounds.Instance;
        if (bounds == null || !bounds.IsValid)
            return GetSpawnPoint(index).position;

        List<Vector3> avoid = new List<Vector3>(otherPlayers);
        foreach (DodgeBall ball in ballSpawner.Balls)
            avoid.Add(ball.transform.position);
        return bounds.FindSpawnPoint(avoid);
    }

    public Transform GetSpawnPoint(int index)
    {
        return spawnPoints[index % spawnPoints.Length];
    }

    public int GetScore(int playerId)
    {
        return scores.TryGetValue(playerId, out int score) ? score : 0;
    }

    public void StartMatch()
    {
        if (!IsAuthoritative)
            return;

        activeRules = (GameSession.Rules ?? new MatchRules()).Clone();
        activeRules.Clamp();
        matchDuration = activeRules.matchDuration;
        pointsPerHit = activeRules.pointsPerHit;
        pointsPerCatch = activeRules.pointsPerCatch;
        pointsPerElimination = activeRules.pointsPerElimination;

        ballSpawner.ResetAll();
        List<Vector3> placed = new List<Vector3>();
        for (int i = 0; i < players.Count; i++)
        {
            Vector3 spawn = GetSpawnPosition(i, placed);
            placed.Add(spawn);
            players[i].ResetForRound(spawn, activeRules.lives);
            players[i].Balls.ThrowPowerMultiplier = activeRules.throwPowerMultiplier;
        }

        foreach (int playerId in scores.Keys.ToList())
            scores[playerId] = 0;

        TimeRemaining = matchDuration;
        Countdown = countdownDuration;
        WinnerId = NoWinner;
        Phase = MatchPhase.Countdown;
        SetPlayersMatchActive(false);
        OnMatchStarted?.Invoke();
    }

    public void PauseMatch()
    {
        if (!IsAuthoritative || Phase != MatchPhase.Playing)
            return;

        Phase = MatchPhase.Paused;
        SetPlayersMatchActive(false);
    }

    public void TogglePause()
    {
        if (Phase == MatchPhase.Playing)
            PauseMatch();
        else if (Phase == MatchPhase.Paused)
            ResumeMatch();
    }

    public void ResumeMatch()
    {
        if (!IsAuthoritative || Phase != MatchPhase.Paused)
            return;

        Phase = MatchPhase.Playing;
        SetPlayersMatchActive(true);
    }

    public void EndMatch()
    {
        if (!IsAuthoritative || Phase == MatchPhase.Ended)
            return;

        WinnerId = DetermineWinner();
        Phase = MatchPhase.Ended;
        SetPlayersMatchActive(false);
        OnMatchEnded?.Invoke(WinnerId);
    }

    public void Shutdown()
    {
        foreach (DodgeballPlayer player in players)
        {
            player.Health.OnHit -= HandlePlayerHit;
            player.Balls.OnBallCaught -= HandleBallCaught;
        }
        players.Clear();
        scores.Clear();
    }

    public void ApplyNetworkState(MatchPhase phase, float timeRemaining, float countdown, int winnerId)
    {
        TimeRemaining = timeRemaining;
        Countdown = countdown;
        WinnerId = winnerId;
        if (phase == Phase)
            return;

        MatchPhase previous = Phase;
        Phase = phase;

        bool newRound = previous == MatchPhase.Waiting || previous == MatchPhase.Ended;
        if (phase == MatchPhase.Countdown || (phase == MatchPhase.Playing && newRound))
            OnMatchStarted?.Invoke();
        else if (phase == MatchPhase.Ended)
            OnMatchEnded?.Invoke(winnerId);
    }

    public void SetNetworkScore(int playerId, int score)
    {
        scores[playerId] = score;
    }

    private void Update()
    {
        if (!IsAuthoritative)
            return;

        if (Phase == MatchPhase.Countdown)
        {
            Countdown = Mathf.Max(0f, Countdown - Time.deltaTime);
            if (Countdown <= 0f)
            {
                Phase = MatchPhase.Playing;
                SetPlayersMatchActive(true);
            }
            return;
        }

        if (Phase != MatchPhase.Playing)
            return;

        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        if (TimeRemaining <= 0f)
            EndMatch();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    private void HandlePlayerHit(PlayerHealth victim, int attackerId)
    {
        if (Phase != MatchPhase.Playing)
            return;

        AddScore(attackerId, pointsPerHit);
        if (!victim.IsEliminated)
            return;

        AddScore(attackerId, pointsPerElimination);
        CheckForWinner();
    }

    private void HandleBallCaught(int catcherId, int throwerId)
    {
        if (Phase == MatchPhase.Playing)
            AddScore(catcherId, pointsPerCatch);
    }

    private void AddScore(int playerId, int points)
    {
        if (scores.ContainsKey(playerId))
            scores[playerId] += points;
    }

    private void CheckForWinner()
    {
        if (players.Count(candidate => !candidate.Health.IsEliminated) <= 1)
            EndMatch();
    }

    private int DetermineWinner()
    {
        List<DodgeballPlayer> survivors = players.Where(candidate => !candidate.Health.IsEliminated).ToList();
        if (survivors.Count == 1)
            return survivors[0].PlayerId;

        IEnumerable<DodgeballPlayer> candidates = survivors.Count > 0 ? survivors : players;
        int bestScore = int.MinValue;
        int winner = NoWinner;

        foreach (DodgeballPlayer player in candidates)
        {
            int score = GetScore(player.PlayerId);
            if (score > bestScore)
            {
                bestScore = score;
                winner = player.PlayerId;
            }
            else if (score == bestScore)
            {
                winner = NoWinner;
            }
        }

        return winner;
    }

    private void SetPlayersMatchActive(bool isActive)
    {
        foreach (DodgeballPlayer player in players)
            player.SetMatchActive(isActive);
    }
}
