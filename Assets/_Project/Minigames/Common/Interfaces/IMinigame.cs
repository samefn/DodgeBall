public interface IMinigame
{
    void Initialize(MinigameContext context);
    void StartMatch();
    void PauseMatch();
    void ResumeMatch();
    void EndMatch();
    void Shutdown();
}
