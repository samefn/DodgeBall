using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using static UIAssets;
using static UIBuilderKit;

public static class LobbyLayout
{
    internal static void Build()
    {
        RectTransform canvas = RequireRect("Canvas");
        SetupScaler(canvas.gameObject);

        RectTransform start = RequireRect("IniciarPartida");
        RectTransform exit = RequireRect("Salir");
        RectTransform players = RequireRect("PlayersText");
        RectTransform role = RequireRect("RoleText");
        ResetGenerated(canvas, start, exit, players, role);

        AddBackground(canvas, RoomsBackground, Color.white);
        NewImage("UI_Titulo", canvas, Title, new Vector2(0.5f, 1f), new Vector2(0f, -130f), 200f);

        Place(role, new Vector2(0.5f, 1f), new Vector2(0f, -275f), new Vector2(1500f, 60f));
        StyleText(role, 34f, TextAlignmentOptions.Center, Color.white);
        role.SetAsLastSibling();

        RectTransform playersCard = NewCard("UI_PlayersCard", canvas, new Vector2(0.5f, 0.5f), new Vector2(-430f, -30f), new Vector2(760f, 500f));
        MoveInto(players, playersCard);
        StretchWithPadding(players, 55f);
        StyleText(players, 30f, TextAlignmentOptions.Top, Color.white);

        NewImage("UI_Reglas", canvas, RulesPanel, new Vector2(0.5f, 0.5f), new Vector2(420f, -30f), 500f);
        RectTransform rulesValues = NewText("UI_RulesValues", canvas, "", 22f, TextAlignmentOptions.Center);
        Place(rulesValues, new Vector2(0.5f, 0.5f), new Vector2(420f, -315f), new Vector2(760f, 40f));
        rulesValues.GetComponent<TMP_Text>().color = new Color(1f, 0.85f, 0.3f);

        LobbyUI lobby = Object.FindFirstObjectByType<LobbyUI>(FindObjectsInactive.Include);
        if (lobby != null)
            SetReferences(lobby, ("rulesText", rulesValues.GetComponent<TMP_Text>()));

        StyleImageButton(start.gameObject, PlayButton, 130f);
        PlaceKeepSize(start, new Vector2(0.5f, 0f), new Vector2(230f, 115f));
        start.SetAsLastSibling();

        StyleImageButton(exit.gameObject, ExitButton, 120f);
        PlaceKeepSize(exit, new Vector2(0.5f, 0f), new Vector2(-230f, 115f));
        exit.SetAsLastSibling();
    }
}
