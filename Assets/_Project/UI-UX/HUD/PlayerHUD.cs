using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private const float PunchDuration = 0.35f;
    private const float LowTimeSeconds = 10f;

    [SerializeField] private DodgeballPlayer player;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text ballText;

    [Header("Throw charge (optional)")]
    [SerializeField] private GameObject chargeBar;
    [Tooltip("Image with Image Type = Filled.")]
    [SerializeField] private Image chargeFill;

    [Header("Image HUD")]
    [Tooltip("Only the values are written (the boxes already show a heart, a clock and a star).")]
    [SerializeField] private bool compactLabels;
    [Tooltip("Box that shakes when a life is lost (optional).")]
    [SerializeField] private RectTransform livesBox;
    [Tooltip("Ball image shown in the PELOTA box while this player holds a ball (replaces the text).")]
    [SerializeField] private GameObject ballIcon;
    [SerializeField] private Color lowTimeColor = new Color(1f, 0.3f, 0.25f);

    private DodgeballMatchController match;
    private int lastLives = -1;
    private float punchTimer;
    private Color timerColor = Color.white;

    private void Start()
    {
        match = FindFirstObjectByType<DodgeballMatchController>();
        if (timerText != null)
            timerColor = timerText.color;
    }

    private void Update()
    {
        UpdateLives();
        if (ballIcon != null)
        {
            ballIcon.SetActive(player.IsHoldingBall);
            ballText.text = string.Empty;
        }
        else if (compactLabels)
            ballText.text = player.IsHoldingBall ? "LISTA" : "—";
        else
            ballText.text = player.IsHoldingBall ? "Pelota: lista" : "Pelota: —";
        UpdateChargeBar();

        if (match == null)
            return;

        timerText.text = TimeFormat.MinutesSeconds(match.TimeRemaining);
        bool lowTime = match.Phase == MatchPhase.Playing && match.TimeRemaining <= LowTimeSeconds;
        timerText.color = lowTime ? lowTimeColor : timerColor;

        int score = match.GetScore(player.PlayerId);
        scoreText.text = compactLabels ? score.ToString() : $"Puntos: {score}";
    }

    private void UpdateLives()
    {
        int lives = player.Health.Lives;
        if (compactLabels)
            livesText.text = player.Health.IsEliminated ? "OUT" : $"x{lives}";
        else
            livesText.text = player.Health.IsEliminated ? "ELIMINADO" : $"Vidas: {lives}";

        bool roundStarting = match != null && (match.Phase == MatchPhase.Countdown || match.Phase == MatchPhase.Waiting);
        if (lastLives >= 0 && lives < lastLives && !roundStarting)
        {
            UIAudio.PlayLifeLost();
            punchTimer = PunchDuration;
        }
        lastLives = lives;

        if (livesBox == null)
            return;

        punchTimer = Mathf.Max(0f, punchTimer - Time.unscaledDeltaTime);
        float punch = punchTimer / PunchDuration;
        livesBox.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(punch * Mathf.PI));
        livesBox.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(punch * Mathf.PI * 6f) * 8f * punch);
    }

    private void UpdateChargeBar()
    {
        if (chargeBar == null || chargeFill == null)
            return;

        bool charging = player.ThrowProgress > 0f && player.IsHoldingBall;
        chargeBar.SetActive(charging);
        chargeFill.fillAmount = player.ThrowCharge;
    }
}
