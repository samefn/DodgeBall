using TMPro;
using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Buttons only the host (or local game) can use.")]
    [SerializeField] private GameObject hostControls;
    [SerializeField] private GameObject adminPanelButton;
    [Tooltip("Optional text label that switches between pausing and resuming.")]
    [SerializeField] private TMP_Text pauseMatchLabel;
    [Tooltip("Optional: shown only while the match is running (PAUSAR image).")]
    [SerializeField] private GameObject pauseMatchButton;
    [Tooltip("Optional: shown only while the match is paused.")]
    [SerializeField] private GameObject resumeMatchButton;

    private DodgeballMatchController match;

    public bool IsOpen => menuPanel.activeSelf || settingsPanel.activeSelf;

    private void Start()
    {
        match = FindFirstObjectByType<DodgeballMatchController>();
        Close();
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (hostControls != null)
            hostControls.SetActive(GameSession.IsAuthoritative);
        if (adminPanelButton != null)
            adminPanelButton.SetActive(NetworkService.Instance != null && NetworkService.Instance.IsHost);
        if (pauseMatchLabel != null && match != null)
            pauseMatchLabel.text = match.Phase == MatchPhase.Paused ? "Reanudar partida (P)" : "Pausar partida (P)";

        bool paused = match != null && match.Phase == MatchPhase.Paused;
        if (pauseMatchButton != null)
            pauseMatchButton.SetActive(!paused);
        if (resumeMatchButton != null)
            resumeMatchButton.SetActive(paused);
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        menuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void Close()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void TogglePauseMatch()
    {
        if (match != null)
            match.TogglePause();
    }

    public void RestartMatch()
    {
        if (match != null)
            match.StartMatch();
        Close();
    }

    public void OpenAdminPanel()
    {
        Close();
        HostAdminPanel.Open();
    }

    public void BackToMainMenu()
    {
        SceneFlow.LeaveSession();
    }

    public void QuitGame()
    {
        if (NetworkService.Instance != null)
            NetworkService.Instance.Shutdown();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
