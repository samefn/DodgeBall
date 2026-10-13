using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArenaOverlayUI : MonoBehaviour
{
    private const float ReferenceHeight = 720f;
    private const float ObjectiveSeconds = 7f;

    private DodgeballMatchController match;
    private PauseMenuUI pauseMenu;
    private float objectiveUntil;
    private GUIStyle bannerStyle;
    private GUIStyle centeredStyle;

    private static NetworkService Network => NetworkService.Instance;
    private bool MenuOpen => pauseMenu != null && pauseMenu.IsOpen;

    public void Initialize(DodgeballMatchController matchController)
    {
        match = matchController;
        pauseMenu = FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        match.OnMatchStarted += ShowObjective;
        ShowObjective();
    }

    private void OnDestroy()
    {
        if (match != null)
            match.OnMatchStarted -= ShowObjective;

        InputGate.Blocked = false;
    }

    private void ShowObjective()
    {
        objectiveUntil = Time.unscaledTime + ObjectiveSeconds;
        if (pauseMenu != null)
            pauseMenu.Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame && pauseMenu != null)
                pauseMenu.Toggle();
            if (keyboard.pKey.wasPressedThisFrame && GameSession.IsAuthoritative)
                match.TogglePause();
        }

        bool menusOpen = MenuOpen || HostAdminPanel.IsOpen;
        bool cursorFree = menusOpen || match.Phase == MatchPhase.Ended;
        InputGate.Blocked = menusOpen;
        Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = cursorFree;
    }

    private void OnGUI()
    {
        if (match == null || MenuOpen)
            return;

        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        UISkin.ApplyIMGUIFont();
        EnsureStyles();

        DrawBanners(width);
        DrawHints(width);
        if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
            DrawScoreboard(width);
    }

    private void DrawBanners(float width)
    {
        switch (match.Phase)
        {
            case MatchPhase.Countdown:
                GUI.Label(new Rect(0f, 330f, width, 80f), $"¡Prepárate! {Mathf.CeilToInt(match.Countdown)}", bannerStyle);
                break;
            case MatchPhase.Paused:
                GUI.Label(new Rect(0f, 330f, width, 80f), "PAUSA", bannerStyle);
                GUI.Label(new Rect(0f, 400f, width, 30f), GameSession.IsAuthoritative ? "Pulsa P para reanudar" : "El host pausó la partida", centeredStyle);
                break;
        }

        if (Time.unscaledTime < objectiveUntil && match.Phase != MatchPhase.Ended)
        {
            Rect box = new Rect(width / 2f - 360f, 150f, 720f, 150f);
            IMGUIDraw.Panel(box);
            GUI.Label(new Rect(box.x + 30f, box.y + 24f, box.width - 60f, 28f), "<size=22>OBJETIVO</size>", centeredStyle);
            GUI.Label(new Rect(box.x + 40f, box.y + 54f, box.width - 80f, 80f), DodgeballMatchController.Objective, centeredStyle);
        }
    }

    private void DrawHints(float width)
    {
        string hints = "Esc: menú   Tab: marcador";
        if (GameSession.IsAuthoritative)
            hints += "   P: pausa";
        if (Network != null && Network.IsHost)
            hints += "   F1: panel del host";
        GUI.Label(new Rect(width / 2f - 300f, ReferenceHeight - 30f, 600f, 25f), hints, centeredStyle);

        if (Network != null && Network.IsClient)
            GUI.Label(new Rect(width - 170f, ReferenceHeight - 30f, 160f, 25f), $"Ping: {Network.GetPing(Network.LocalPlayerId)} ms", IMGUIControls.RichLabel());
    }

    private void DrawScoreboard(float width)
    {
        List<DodgeballPlayer> players = new List<DodgeballPlayer>(match.Players);
        players.Sort((first, second) => match.GetScore(second.PlayerId).CompareTo(match.GetScore(first.PlayerId)));

        Rect box = new Rect(width / 2f - 340f, 150f, 680f, 110f + players.Count * 28f);
        IMGUIDraw.Panel(box);
        GUILayout.BeginArea(new Rect(box.x + 40f, box.y + 30f, box.width - 80f, box.height - 60f));
        GUILayout.Label($"<b>Marcador</b>   Tiempo: {TimeFormat.MinutesSeconds(match.TimeRemaining)}   Estado: {MatchLabels.PhaseName(match.Phase)}", IMGUIControls.RichLabel());
        foreach (DodgeballPlayer player in players)
        {
            string lives = player.Health.IsEliminated ? "eliminado" : $"{player.Health.Lives} vidas";
            GUILayout.Label($"{MatchLabels.ColoredName(player.PlayerId)} ({PlayerIdentity.ColorNameFor(player.PlayerId)})   {lives}   {match.GetScore(player.PlayerId)} pts   {PingText(player.PlayerId)}", IMGUIControls.RichLabel());
        }
        GUILayout.EndArea();
    }

    private static string PingText(int playerId)
    {
        if (Network == null || !Network.IsActive)
            return string.Empty;
        if (playerId == NetworkService.HostPlayerId)
            return "(host)";

        int ping = Network.GetPing(playerId);
        return ping >= 0 ? $"{ping} ms" : "...";
    }

    private void EnsureStyles()
    {
        if (bannerStyle != null)
            return;

        bannerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        centeredStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            richText = true
        };
    }
}
