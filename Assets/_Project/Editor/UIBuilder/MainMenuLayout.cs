using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static UIAssets;
using static UIBuilderKit;

public static class MainMenuLayout
{
    internal static void Build()
    {
        RectTransform canvas = RequireRect("Canvas");
        SetupScaler(canvas.gameObject);
        MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menu == null)
            throw new InvalidOperationException("MainMenu: no se encontró MainMenuUI.");

        RectTransform createRoom = RequireRect("CrearSala");
        RectTransform joinRoom = RequireRect("Unirse_a_Sala");
        RectTransform exit = RequireRect("Salir");
        RectTransform playLocal = RequireRect("JugarLocal");
        RectTransform roomInput = RequireRect("RoomNameInput");
        RectTransform roomsText = RequireRect("RoomsText");
        RectTransform statusText = RequireRect("StatusText");
        ResetGenerated(canvas, createRoom, joinRoom, exit, playLocal, roomInput, roomsText, statusText);

        GameObject oldTitle = FindInScene("Titulo");
        if (oldTitle != null)
            oldTitle.SetActive(false);

        RectTransform mainPanel = NewPanel("UI_MainPanel", canvas);
        AddBackground(mainPanel, MainMenuBackground, Color.white);
        RectTransform mainColumn = NewColumn("UI_MainButtons", mainPanel, new Vector2(0.72f, 0.3f), Vector2.zero, 12f);
        NewImageButton("UI_Jugar", mainColumn, PlayButton, 135f, menu.ShowRooms);
        MoveInto(playLocal, mainColumn);
        StyleImageButton(playLocal.gameObject, PlayLocalButton, 85f);
        NewImageButton("UI_Configuracion", mainColumn, SettingsButton, 125f, menu.ShowSettings);
        MoveInto(exit, mainColumn);
        StyleImageButton(exit.gameObject, ExitButton, 125f);

        RectTransform roomsPanel = NewPanel("UI_RoomsPanel", canvas);
        AddBackground(roomsPanel, RoomsBackground, Color.white);
        NewImage("UI_Titulo", roomsPanel, Title, new Vector2(0.5f, 1f), new Vector2(0f, -150f), 230f);

        RectTransform roomsColumn = NewColumn("UI_RoomButtons", roomsPanel, new Vector2(0.5f, 0.5f), new Vector2(-230f, -110f), 22f);
        MoveInto(roomInput, roomsColumn);
        StyleRoomInput(roomInput.gameObject);
        MoveInto(createRoom, roomsColumn);
        StyleImageButton(createRoom.gameObject, CreateRoomButton, 100f);
        MoveInto(joinRoom, roomsColumn);
        StyleImageButton(joinRoom.gameObject, JoinRoomButton, 100f);
        NewImageButton("UI_VolverAlMenu", roomsColumn, BackToMenuButton, 95f, menu.ShowMain);

        RectTransform roomsCard = NewCard("UI_RoomsListCard", roomsPanel, new Vector2(0.5f, 0.5f), new Vector2(440f, -110f), new Vector2(560f, 420f));
        MoveInto(roomsText, roomsCard);
        StretchWithPadding(roomsText, 50f);
        StyleText(roomsText, 28f, TextAlignmentOptions.TopLeft, Color.white);

        RectTransform statusCard = NewCard("UI_StatusCard", roomsPanel, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1500f, 120f), 3.6f);
        MoveInto(statusText, statusCard);
        StretchWithPadding(statusText, 32f);
        StyleText(statusText, 28f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));

        GameObject settingsPanel = SettingsPanelLayout.Build(canvas, mainPanel.gameObject, Color.white);

        roomsPanel.gameObject.SetActive(false);
        settingsPanel.SetActive(false);

        SetReferences(menu,
            ("mainPanel", mainPanel.gameObject),
            ("roomsPanel", roomsPanel.gameObject),
            ("settingsPanel", settingsPanel));
    }

    private static void StyleRoomInput(GameObject inputObject)
    {
        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        if (input == null)
            throw new InvalidOperationException("RoomNameInput no tiene TMP_InputField.");

        Image image = GetOrAdd<Image>(inputObject);
        image.sprite = RoomNameInput;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        input.targetGraphic = image;
        input.colors = ButtonColors();

        Vector2 size = SizeForHeight(RoomNameInput, 106f);
        RectTransform rect = (RectTransform)inputObject.transform;
        rect.sizeDelta = size;

        if (input.textViewport != null)
        {
            input.textViewport.anchorMin = Vector2.zero;
            input.textViewport.anchorMax = Vector2.one;
            input.textViewport.offsetMin = new Vector2(size.x * 0.19f, size.y * 0.3f);
            input.textViewport.offsetMax = new Vector2(-size.x * 0.1f, -size.y * 0.3f);
        }

        if (input.textComponent != null)
        {
            input.textComponent.color = Color.white;
            input.textComponent.fontSize = 30f;
            input.textComponent.enableAutoSizing = false;
            input.textComponent.alignment = TextAlignmentOptions.Left;
        }

        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.text = "Nombre de la sala o IP del host";
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            placeholder.fontSize = 28f;
            placeholder.enableAutoSizing = false;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Left;
        }

        AddLayoutSize(inputObject, size);
    }
}
