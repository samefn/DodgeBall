using UnityEngine.SceneManagement;

public static class SceneFlow
{
    public static void ReturnToMainMenu()
    {
        InputGate.Blocked = false;
        GameSession.Mode = GameMode.Local;
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public static void LeaveSession()
    {
        if (NetworkService.Instance != null)
            NetworkService.Instance.Shutdown();

        ReturnToMainMenu();
    }
}
