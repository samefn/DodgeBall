using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] private DodgeballMatchController match;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text scoresText;
    [SerializeField] private Button rematchButton;
    [Tooltip("Optional image that shows the VICTORIA / DERROTA banner (sprites from UISkin).")]
    [SerializeField] private Image resultBanner;

    private void Start()
    {
        panel.SetActive(false);
        rematchButton.gameObject.SetActive(GameSession.IsAuthoritative);
        match.OnMatchEnded += Show;
        match.OnMatchStarted += Hide;
    }

    private void OnDestroy()
    {
        match.OnMatchEnded -= Show;
        match.OnMatchStarted -= Hide;
    }

    public void Rematch()
    {
        match.StartMatch();
    }

    public void ExitToMenu()
    {
        SceneFlow.LeaveSession();
    }

    private void Show(int winnerId)
    {
        panel.SetActive(true);
        bool tie = winnerId == DodgeballMatchController.NoWinner;
        winnerText.text = tie ? "¡Empate!" : $"¡Gana el {PlayerIdentity.DisplayNameFor(winnerId)}!";
        ShowBanner(tie, LocalPlayerWon(winnerId));

        StringBuilder scores = new StringBuilder();
        foreach (DodgeballPlayer player in match.Players)
            scores.AppendLine($"Jugador {player.PlayerId}: {match.GetScore(player.PlayerId)} pts");
        scoresText.text = scores.ToString();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static bool LocalPlayerWon(int winnerId)
    {
        if (winnerId == DodgeballMatchController.NoWinner)
            return false;
        if (!GameSession.IsOnline)
            return true;

        NetworkService network = NetworkService.Instance;
        return network != null && network.LocalPlayerId == winnerId;
    }

    private void ShowBanner(bool tie, bool won)
    {
        UISkin skin = UISkin.Instance;
        if (resultBanner != null)
        {
            Sprite sprite = skin == null || tie ? null : won ? skin.victoryBanner : skin.defeatBanner;
            resultBanner.sprite = sprite;
            resultBanner.enabled = sprite != null;
        }

        if (tie)
            return;
        if (won)
            UIAudio.PlayVictory();
        else
            UIAudio.PlayDefeat();
    }

    private void Hide()
    {
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
