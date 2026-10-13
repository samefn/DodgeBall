public class MinigameContext
{
    public bool IsAuthoritative { get; }

    public MinigameContext(bool isAuthoritative)
    {
        IsAuthoritative = isAuthoritative;
    }
}
