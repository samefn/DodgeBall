public static class GameSession
{
    public static GameMode Mode { get; set; } = GameMode.Local;
    public static bool IsAuthoritative => Mode != GameMode.Client;
    public static bool IsOnline => Mode != GameMode.Local;

    public static MatchRules Rules { get; set; } = new MatchRules();

    public static string LastDisconnectReason { get; set; }
}
