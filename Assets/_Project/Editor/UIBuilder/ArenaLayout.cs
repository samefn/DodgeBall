using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static UIAssets;
using static UIBuilderKit;

public static class ArenaLayout
{
    internal static void Build()
    {
        BuildResults();
        BuildPauseMenu();
    }

    private static void BuildResults()
    {
        RectTransform canvas = RequireRect("ResultsCanvas");
        SetupScaler(canvas.gameObject);

        RectTransform panel = RequireRect("ResultsPanel");
        RectTransform winner = RequireRect("WinnerText");
        RectTransform scores = RequireRect("ScoresText");
        RectTransform rematch = RequireRect("RematchButton");
        RectTransform exit = RequireRect("ExitButton");
        ResetGenerated(panel, winner, scores, rematch, exit);

        Stretch(panel);
        Image panelImage = GetOrAdd<Image>(panel.gameObject);
        panelImage.sprite = null;
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        Vector2 bannerSize = SizeForHeight(VictoryBanner, 300f);
        RectTransform banner = NewUI("UI_ResultBanner", panel);
        Place(banner, new Vector2(0.5f, 1f), new Vector2(0f, -200f), bannerSize);
        Image bannerImage = banner.gameObject.AddComponent<Image>();
        bannerImage.sprite = VictoryBanner;
        bannerImage.preserveAspect = true;
        bannerImage.raycastTarget = false;

        MoveInto(winner, banner);
        winner.anchorMin = Vector2.zero;
        winner.anchorMax = Vector2.one;
        winner.pivot = new Vector2(0.5f, 0.5f);
        winner.offsetMin = new Vector2(bannerSize.x * 0.2f, bannerSize.y * 0.24f);
        winner.offsetMax = new Vector2(-bannerSize.x * 0.2f, -bannerSize.y * 0.5f);
        StyleText(winner, 44f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        TMP_Text winnerLabel = winner.GetComponent<TMP_Text>();
        if (winnerLabel != null)
        {
            winnerLabel.enableAutoSizing = true;
            winnerLabel.fontSizeMin = 20f;
            winnerLabel.fontSizeMax = 46f;
        }

        ResultsUI results = Object.FindFirstObjectByType<ResultsUI>(FindObjectsInactive.Include);
        if (results != null)
            SetReferences(results, ("resultBanner", bannerImage));

        RectTransform scoresCard = NewCard("UI_ScoresCard", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1050f, 380f));
        MoveInto(scores, scoresCard);
        StretchWithPadding(scores, 55f);
        StyleText(scores, 34f, TextAlignmentOptions.Top, Color.white);

        StyleImageButton(rematch.gameObject, RematchButton, 115f);
        PlaceKeepSize(rematch, new Vector2(0.5f, 0f), new Vector2(230f, 120f));
        rematch.SetAsLastSibling();

        StyleImageButton(exit.gameObject, BackToMenuButton, 95f);
        PlaceKeepSize(exit, new Vector2(0.5f, 0f), new Vector2(-260f, 120f));
        exit.SetAsLastSibling();
    }

    private static void BuildPauseMenu()
    {
        GameObject previous = FindInScene("UI_PauseCanvas");
        if (previous != null)
            Object.DestroyImmediate(previous);

        GameObject canvasObject = new GameObject("UI_PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        SetupScaler(canvasObject);
        RectTransform root = (RectTransform)canvasObject.transform;
        PauseMenuUI pause = canvasObject.AddComponent<PauseMenuUI>();

        RectTransform menuPanel = NewPanel("UI_PauseMenu", root);
        AddBackground(menuPanel, RoomsBackground, new Color(0.6f, 0.6f, 0.68f, 1f));
        NewImage("UI_Titulo", menuPanel, Title, new Vector2(0.5f, 1f), new Vector2(0f, -140f), 210f);

        RectTransform column = NewColumn("UI_PauseButtons", menuPanel, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), 18f);
        NewImageButton("UI_Continuar", column, ContinueButton, 95f, pause.Close);
        NewImageButton("UI_Configuracion", column, PauseSettingsButton, 95f, pause.OpenSettings);
        NewImageButton("UI_VolverAlMenu", column, BackToMenuButton, 95f, pause.BackToMainMenu);
        NewImageButton("UI_SalirDelJuego", column, QuitGameButton, 95f, pause.QuitGame);

        RectTransform hostControls = NewUI("UI_HostControls", menuPanel);
        Place(hostControls, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1400f, 90f));
        HorizontalLayoutGroup row = hostControls.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleCenter;
        row.spacing = 20f;
        row.childControlWidth = row.childControlHeight = false;
        row.childForceExpandWidth = row.childForceExpandHeight = false;

        Button pauseMatch = NewImageButton("UI_PausarPartida", hostControls, PauseMatchButton, 80f, pause.TogglePauseMatch);
        Button resumeMatch = NewTextButton("UI_ReanudarPartida", hostControls, "Reanudar partida (P)", new Vector2(380f, 60f), pause.TogglePauseMatch);
        NewImageButton("UI_ReiniciarPartida", hostControls, RestartButton, 80f, pause.RestartMatch);
        Button adminPanel = NewImageButton("UI_PanelHost", hostControls, HostPanelButton, 80f, pause.OpenAdminPanel);

        GameObject settingsPanel = SettingsPanelLayout.Build(root, menuPanel.gameObject, new Color(0.6f, 0.6f, 0.68f, 1f));

        menuPanel.gameObject.SetActive(false);
        settingsPanel.SetActive(false);

        SetReferences(pause,
            ("menuPanel", menuPanel.gameObject),
            ("settingsPanel", settingsPanel),
            ("hostControls", hostControls.gameObject),
            ("adminPanelButton", adminPanel.gameObject),
            ("pauseMatchButton", pauseMatch.gameObject),
            ("resumeMatchButton", resumeMatch.gameObject));
    }
}
