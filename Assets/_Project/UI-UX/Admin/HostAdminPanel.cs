using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HostAdminPanel : MonoBehaviour
{
    private const float ReferenceHeight = 720f;

    private static readonly int[] DurationPresets = { 3, 5, 10 };

    private bool isOpen;
    private Vector2 scroll;
    private MatchRules editingRules;
    private string notice;
    private float noticeUntil;
    private DodgeballMatchController match;
    private Action pendingAction;

    public static bool IsOpen { get; private set; }

    private static NetworkService Network => NetworkService.Instance;

    private void Update()
    {
        bool canOpen = Network != null && Network.IsHost;
        if (!canOpen)
            isOpen = false;
        else if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            Toggle();

        IsOpen = isOpen;

        Action action = pendingAction;
        pendingAction = null;
        action?.Invoke();
    }

    public static void Open()
    {
        HostAdminPanel panel = FindFirstObjectByType<HostAdminPanel>();
        if (panel != null && !panel.isOpen)
            panel.Toggle();
    }

    private void Toggle()
    {
        isOpen = !isOpen;
        if (isOpen)
            editingRules = GameSession.Rules.Clone();
    }

    private void OnGUI()
    {
        if (!isOpen || Network == null || !Network.IsHost)
            return;

        UISkin.ApplyIMGUIFont();
        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        GUI.depth = -10;

        float screenWidth = Screen.width / scale;
        float width = Mathf.Min(980f, screenWidth - 20f);
        float height = 600f;
        Rect area = new Rect((screenWidth - width) / 2f, (ReferenceHeight - height) / 2f, width, height);
        float columnWidth = (width - 110f) / 2f;

        IMGUIDraw.Panel(area);
        GUILayout.BeginArea(new Rect(area.x + 40f, area.y + 32f, area.width - 80f, area.height - 64f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("<size=22>Panel del Host</size>", IMGUIControls.RichLabel());
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Sala: {Network.RoomName}   ·   Puerto TCP: {Network.Port}   ·   F1 para cerrar", IMGUIControls.RichLabel());
        GUILayout.EndHorizontal();
        GUILayout.Space(6f);

        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(columnWidth));
        DrawPlayers();
        GUILayout.Space(10f);
        DrawMatchControls();
        GUILayout.Space(10f);
        DrawBans();
        GUILayout.EndVertical();

        GUILayout.Space(20f);

        GUILayout.BeginVertical(GUILayout.Width(columnWidth));
        DrawRules();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (!string.IsNullOrEmpty(notice) && Time.unscaledTime < noticeUntil)
            GUILayout.Label($"<color=yellow>{notice}</color>", IMGUIControls.RichLabel());
        GUILayout.FlexibleSpace();
        if (IMGUIControls.ImageButton(Skin?.closePanelButton, "Cerrar panel", 40f))
            isOpen = false;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawPlayers()
    {
        GUILayout.Label("<b>Jugadores conectados</b>", IMGUIControls.RichLabel());
        foreach (int playerId in new List<int>(Network.LobbyPlayerIds))
        {
            bool isHost = playerId == NetworkService.HostPlayerId;
            int ping = Network.GetPing(playerId);
            string pingText = isHost ? "host" : ping >= 0 ? $"{ping} ms" : "...";
            string address = isHost ? "local" : Network.GetPlayerAddress(playerId);

            GUILayout.Label($"{MatchLabels.ColoredName(playerId)}  |  Ping: {pingText}  |  {address}", IMGUIControls.RichLabel());
            GUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            if (!isHost)
            {
                if (IMGUIControls.ImageButton(Skin?.kickButton, "Expulsar", 32f))
                {
                    pendingAction = () => Network.Kick(playerId, "El host te expulsó de la sala.");
                    ShowNotice($"{PlayerIdentity.DisplayNameFor(playerId)} fue expulsado.");
                }
                if (IMGUIControls.ImageButton(Skin?.banButton, "Banear IP", 32f))
                {
                    pendingAction = () => Network.Ban(playerId);
                    ShowNotice(IsLoopback(address)
                        ? $"IP {address} baneada. Es una IP local: afecta a todos los clientes de este PC."
                        : $"IP {address} baneada permanentemente.");
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawMatchControls()
    {
        GUILayout.Label("<b>Partida</b>", IMGUIControls.RichLabel());
        if (match == null)
            match = FindFirstObjectByType<DodgeballMatchController>();

        if (match == null)
        {
            GUILayout.Label("Inicia la partida desde el lobby para poder pausarla o reiniciarla.");
            return;
        }

        GUILayout.Label($"Estado: {MatchLabels.PhaseName(match.Phase)}");
        GUILayout.BeginHorizontal();
        if (match.Phase == MatchPhase.Playing && IMGUIControls.ImageButton(Skin?.pauseButton, "Pausar (P)", 38f))
            pendingAction = match.PauseMatch;
        if (match.Phase == MatchPhase.Paused && IMGUIControls.TextButton("Reanudar (P)", GUI.skin.button))
            pendingAction = match.ResumeMatch;
        if (IMGUIControls.ImageButton(Skin?.restartButton, "Reiniciar partida", 38f))
            pendingAction = match.StartMatch;
        if ((match.Phase == MatchPhase.Playing || match.Phase == MatchPhase.Paused) && IMGUIControls.ImageButton(Skin?.endNowButton, "Terminar ahora", 38f))
            pendingAction = match.EndMatch;
        GUILayout.EndHorizontal();
    }

    private void DrawRules()
    {
        GUILayout.Label("<b>Reglas</b> (se aplican al iniciar la siguiente partida)", IMGUIControls.RichLabel());
        if (editingRules == null)
            editingRules = GameSession.Rules.Clone();

        editingRules.matchDuration = Mathf.Round(IMGUIControls.BallSlider($"Duración: {TimeFormat.MinutesSeconds(editingRules.matchDuration)} min", editingRules.matchDuration, MatchRules.MinDuration, MatchRules.MaxDuration) / 10f) * 10f;
        GUILayout.BeginHorizontal();
        foreach (int minutes in DurationPresets)
        {
            bool selected = Mathf.Approximately(editingRules.matchDuration, minutes * 60f);
            string label = selected ? $"<color=yellow>{minutes} min</color>" : $"{minutes} min";
            if (IMGUIControls.TextButton(label, IMGUIControls.RichButton(), GUILayout.Height(28f)))
                editingRules.matchDuration = minutes * 60f;
        }
        GUILayout.EndHorizontal();
        editingRules.lives = Mathf.RoundToInt(IMGUIControls.BallSlider($"Vidas por jugador: {editingRules.lives}", editingRules.lives, MatchRules.MinLives, MatchRules.MaxLives));
        editingRules.pointsPerHit = Mathf.RoundToInt(IMGUIControls.BallSlider($"Puntos por golpe: {editingRules.pointsPerHit}", editingRules.pointsPerHit, 0, 10));
        editingRules.pointsPerCatch = Mathf.RoundToInt(IMGUIControls.BallSlider($"Puntos por atrapar: {editingRules.pointsPerCatch}", editingRules.pointsPerCatch, 0, 10));
        editingRules.pointsPerElimination = Mathf.RoundToInt(IMGUIControls.BallSlider($"Puntos por eliminar: {editingRules.pointsPerElimination}", editingRules.pointsPerElimination, 0, 10));
        editingRules.throwPowerMultiplier = Mathf.Round(IMGUIControls.BallSlider($"Potencia de lanzamiento: x{editingRules.throwPowerMultiplier:0.0}", editingRules.throwPowerMultiplier, 0.5f, 2f) * 10f) / 10f;

        GUILayout.BeginHorizontal();
        if (IMGUIControls.ImageButton(Skin?.applyRulesButton, "Aplicar reglas", 40f))
        {
            editingRules.Clamp();
            GameSession.Rules = editingRules.Clone();
            Network.BroadcastRules();
            ShowNotice("Reglas enviadas a todos los jugadores.");
        }
        if (IMGUIControls.ImageButton(Skin?.defaultRulesButton, "Valores por defecto", 40f))
            editingRules = new MatchRules();
        GUILayout.EndHorizontal();
    }

    private void DrawBans()
    {
        GUILayout.Label("<b>IPs baneadas</b> (se guardan aunque cierres el juego)", IMGUIControls.RichLabel());
        IReadOnlyCollection<string> banned = BanList.Addresses;
        if (banned.Count == 0)
        {
            GUILayout.Label("Ninguna.");
            return;
        }

        foreach (string address in banned)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(address, GUILayout.Width(270f));
            if (IMGUIControls.TextButton("Quitar baneo", GUI.skin.button))
                pendingAction = () => BanList.Unban(address);
            GUILayout.EndHorizontal();
        }
    }

    private static UISkin Skin => UISkin.Instance;

    private void ShowNotice(string message)
    {
        notice = message;
        noticeUntil = Time.unscaledTime + 5f;
    }

    private static bool IsLoopback(string address)
    {
        return address == "127.0.0.1" || address == "::1";
    }
}
