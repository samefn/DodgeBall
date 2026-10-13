using UnityEngine;

public static class MatchLabels
{
    public static string ColoredName(int playerId)
    {
        string color = ColorUtility.ToHtmlStringRGB(PlayerIdentity.ColorFor(playerId));
        return $"<color=#{color}><b>{PlayerIdentity.DisplayNameFor(playerId)}</b></color>";
    }

    public static string PhaseName(MatchPhase phase)
    {
        switch (phase)
        {
            case MatchPhase.Countdown: return "Cuenta regresiva";
            case MatchPhase.Playing: return "En juego";
            case MatchPhase.Paused: return "Pausada";
            case MatchPhase.Ended: return "Terminada";
            default: return "Esperando";
        }
    }
}
