using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private Button startButton;
    [Tooltip("Optional: line under the REGLAS image with the values chosen by the host.")]
    [SerializeField] private TMP_Text rulesText;

    private NetworkService network;
    private bool loadingArena;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        network = NetworkService.Instance;
        if (network == null || !network.IsActive)
        {
            SceneFlow.ReturnToMainMenu();
            return;
        }

        startButton.gameObject.SetActive(network.IsHost);
        network.OnLobbyChanged += Refresh;
        network.OnRulesChanged += Refresh;
        network.OnMessage += HandleMessage;
        network.OnDisconnectedFromHost += SceneFlow.ReturnToMainMenu;
        Refresh();
    }

    private void OnDestroy()
    {
        if (network == null)
            return;

        network.OnLobbyChanged -= Refresh;
        network.OnRulesChanged -= Refresh;
        network.OnMessage -= HandleMessage;
        network.OnDisconnectedFromHost -= SceneFlow.ReturnToMainMenu;
    }

    public void StartMatch()
    {
        if (!network.IsHost)
            return;

        network.Send(MessageType.MatchStart);
        LoadArena();
    }

    public void LeaveLobby()
    {
        SceneFlow.LeaveSession();
    }

    private void HandleMessage(NetworkMessage message)
    {
        if (message.type == MessageType.MatchStart)
            LoadArena();
    }

    private void Refresh()
    {
        string room = $"Sala \"{network.RoomName}\"";
        if (network.IsHost)
            roleText.text = $"{room} · Eres el HOST (F1: panel) · Tu IP: {string.Join(" / ", LanRoomDiscovery.GetLocalIPv4Addresses())}";
        else
            roleText.text = network.LocalPlayerId > 0 ? $"{room} · Eres el {PlayerIdentity.DisplayNameFor(network.LocalPlayerId)}" : "Conectando...";

        StringBuilder text = new StringBuilder();
        text.AppendLine(string.Join("\n", network.LobbyPlayerIds.Select(FormatPlayer)));
        text.AppendLine();
        MatchRules rules = GameSession.Rules;
        string rulesLine = $"{TimeFormat.MinutesSeconds(rules.matchDuration)} min · {rules.lives} vidas · golpe {rules.pointsPerHit} pt · atrapar {rules.pointsPerCatch} pt · eliminar {rules.pointsPerElimination} pt";
        if (rulesText != null)
        {
            rulesText.text = $"Valores de esta sala: {rulesLine}";
        }
        else
        {
            text.AppendLine($"Reglas: {rulesLine}");
            if (network.IsHost && network.LobbyPlayerIds.Count < 2)
                text.AppendLine("Esperando a que se conecte al menos otro jugador (puedes iniciar igualmente).");
        }
        if (!network.IsHost)
            text.AppendLine(network.LobbyPlayerIds.Count < 2 ? "Esperando jugadores..." : "Esperando a que el host inicie la partida...");
        playersText.text = text.ToString();

        if (network.IsClient && network.LocalPlayerId > 0 && network.HostMatchInProgress)
            LoadArena();
    }

    private string FormatPlayer(int playerId)
    {
        string host = playerId == NetworkService.HostPlayerId ? " (host)" : string.Empty;
        string you = playerId == network.LocalPlayerId ? " (tú)" : string.Empty;
        int ping = network.GetPing(playerId);
        string pingText = playerId == NetworkService.HostPlayerId ? string.Empty : ping >= 0 ? $" · {ping} ms" : string.Empty;
        string color = ColorUtility.ToHtmlStringRGB(PlayerIdentity.ColorFor(playerId));
        return $"<color=#{color}>{PlayerIdentity.DisplayNameFor(playerId)} ({PlayerIdentity.ColorNameFor(playerId)})</color>{host}{you}{pingText}";
    }

    private void LoadArena()
    {
        if (loadingArena)
            return;

        loadingArena = true;
        SceneManager.LoadScene(SceneNames.DodgeballArena);
    }
}
