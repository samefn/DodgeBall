using System.Collections.Generic;
using UnityEngine;

public class DodgeballArenaController : MonoBehaviour
{
    private const string PlayerOneBodyLayer = "PlayerBody1";
    private const string PlayerTwoBodyLayer = "PlayerBody2";
    private static readonly Rect FullScreen = new Rect(0f, 0f, 1f, 1f);
    private static readonly Rect TopHalf = new Rect(0f, 0.5f, 1f, 0.5f);
    private static readonly Rect BottomHalf = new Rect(0f, 0f, 1f, 0.5f);

    [SerializeField] private DodgeballPlayer playerPrefab;
    [SerializeField] private DodgeballMatchController match;
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] private KeyboardProfile playerOneProfile;
    [SerializeField] private KeyboardProfile playerTwoProfile;

    private readonly Dictionary<int, DodgeballPlayer> players = new Dictionary<int, DodgeballPlayer>();
    private int spawnCounter;

    private static NetworkService Network => NetworkService.Instance;

    public IEnumerable<int> PlayerIds => players.Keys;

    private void Awake()
    {
        if (!TryGetComponent(out ArenaBounds _))
            gameObject.AddComponent<ArenaBounds>();
    }

    private void Start()
    {
        if (GameSession.IsOnline && (Network == null || !Network.IsActive))
        {
            SceneFlow.ReturnToMainMenu();
            return;
        }

        bool authoritative = GameSession.IsAuthoritative;

        ballSpawner.SetAuthoritative(authoritative);
        match.Initialize(new MinigameContext(authoritative));
        gameObject.AddComponent<ArenaOverlayUI>().Initialize(match);

        switch (GameSession.Mode)
        {
            case GameMode.Local:
                StartLocalMatch();
                break;
            case GameMode.Host:
                StartHostMatch();
                break;
            case GameMode.Client:
                Network.OnDisconnectedFromHost += SceneFlow.ReturnToMainMenu;
                break;
        }
    }

    private void OnDestroy()
    {
        if (Network == null)
            return;

        Network.OnPlayerJoined -= HandlePlayerJoined;
        Network.OnPlayerLeft -= RemovePlayer;
        Network.OnDisconnectedFromHost -= SceneFlow.ReturnToMainMenu;
    }

    public DodgeballPlayer GetPlayer(int playerId)
    {
        return players.TryGetValue(playerId, out DodgeballPlayer player) ? player : null;
    }

    public DodgeballPlayer GetOrCreateView(int playerId)
    {
        if (players.TryGetValue(playerId, out DodgeballPlayer existing))
            return existing;

        DodgeballPlayer player = SpawnPlayer(playerId);
        player.ConfigureAsNetworkView(playerId == Network.LocalPlayerId, playerOneProfile, PlayerOneBodyLayer);
        return player;
    }

    public void RemovePlayer(int playerId)
    {
        if (!players.TryGetValue(playerId, out DodgeballPlayer player))
            return;

        player.Balls.ReleaseBall();
        players.Remove(playerId);
        match.UnregisterPlayer(playerId);
        Destroy(player.gameObject);
    }

    private void StartLocalMatch()
    {
        SpawnPlayer(1).ConfigureAsLocal(playerOneProfile, TopHalf, true, PlayerOneBodyLayer);
        SpawnPlayer(2).ConfigureAsLocal(playerTwoProfile, BottomHalf, false, PlayerTwoBodyLayer);
        match.StartMatch();
    }

    private void StartHostMatch()
    {
        SpawnPlayer(Network.LocalPlayerId).ConfigureAsLocal(playerOneProfile, FullScreen, true, PlayerOneBodyLayer);
        foreach (int playerId in Network.RemotePlayerIds)
            SpawnPlayer(playerId).ConfigureAsRemote();

        Network.OnPlayerJoined += HandlePlayerJoined;
        Network.OnPlayerLeft += RemovePlayer;
        Network.SetMatchInProgress(true);
        match.StartMatch();
    }

    private void HandlePlayerJoined(int playerId)
    {
        if (players.ContainsKey(playerId))
            return;

        SpawnPlayer(playerId).ConfigureAsRemote();
        Network.SendTo(playerId, MessageType.MatchStart);
    }

    private DodgeballPlayer SpawnPlayer(int playerId)
    {
        List<Vector3> others = new List<Vector3>();
        foreach (DodgeballPlayer other in match.Players)
            others.Add(other.transform.position);
        Vector3 position = match.GetSpawnPosition(spawnCounter, others);
        Quaternion rotation = ArenaBounds.Instance != null && ArenaBounds.Instance.IsValid
            ? ArenaBounds.Instance.FacingCenter(position)
            : match.GetSpawnPoint(spawnCounter).rotation;
        spawnCounter++;
        DodgeballPlayer player = Instantiate(playerPrefab, position, rotation);
        player.Identity.Assign(playerId);
        player.ApplyIdentityVisuals();
        players.Add(playerId, player);
        match.RegisterPlayer(player);
        return player;
    }
}
