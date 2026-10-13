using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIAssets;
using static UIBuilderKit;

public static class SettingsPanelLayout
{
    internal static GameObject Build(Transform parent, GameObject returnPanel, Color backgroundTint)
    {
        RectTransform panel = NewPanel("UI_SettingsPanel", parent);
        AddBackground(panel, RoomsBackground, backgroundTint);
        NewImage("UI_Titulo", panel, Title, new Vector2(0.5f, 1f), new Vector2(0f, -140f), 210f);
        SettingsPanelUI settings = panel.gameObject.AddComponent<SettingsPanelUI>();

        RectTransform card = NewCard("UI_SettingsCard", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1150f, 640f));
        RectTransform column = NewColumn("UI_SettingsRows", card, new Vector2(0.5f, 0.5f), Vector2.zero, 26f);

        RectTransform header = NewText("UI_Header", column, "CONFIGURACIÓN", 54f, TextAlignmentOptions.Center);
        header.sizeDelta = new Vector2(1000f, 70f);
        ((TMP_Text)header.GetComponent<TextMeshProUGUI>()).fontStyle = FontStyles.Bold;

        Slider sensitivity = NewSliderRow(column, "UI_Sensibilidad", "Sensibilidad del ratón", out TMP_Text sensitivityValue);
        Slider volume = NewSliderRow(column, "UI_Volumen", "Volumen", out TMP_Text volumeValue);
        Button fullscreen = NewImageButton("UI_PantallaCompleta", column, FullscreenButton, 190f, settings.ToggleFullscreen);
        TMP_Text fullscreenValue = NewFullscreenValue(fullscreen.transform);
        NewImageButton("UI_Cerrar", column, CloseButton, 95f, settings.Back);

        SetReferences(settings,
            ("sensitivitySlider", sensitivity),
            ("sensitivityValue", sensitivityValue),
            ("volumeSlider", volume),
            ("volumeValue", volumeValue),
            ("fullscreenLabel", fullscreenValue),
            ("returnPanel", returnPanel));
        SerializedObject settingsData = new SerializedObject(settings);
        settingsData.FindProperty("compactFullscreenLabel").boolValue = true;
        settingsData.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
    }

    private static Slider NewSliderRow(Transform parent, string name, string label, out TMP_Text valueText)
    {
        RectTransform row = NewUI(name, parent);
        row.sizeDelta = new Vector2(1000f, 60f);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 25f;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        RectTransform labelRect = NewText("Label", row, label, 32f, TextAlignmentOptions.Right);
        labelRect.sizeDelta = new Vector2(380f, 60f);

        DefaultControls.Resources resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };
        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = "Slider";
        sliderObject.transform.SetParent(row, false);
        SetLayerRecursively(sliderObject, LayerMask.NameToLayer("UI"));
        ((RectTransform)sliderObject.transform).sizeDelta = new Vector2(440f, 28f);

        Transform fill = sliderObject.transform.Find("Fill Area/Fill");
        if (fill != null)
            fill.GetComponent<Image>().color = new Color(0.9f, 0.15f, 0.15f);
        Transform background = sliderObject.transform.Find("Background");
        if (background != null)
            background.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.45f);

        Transform handleArea = sliderObject.transform.Find("Handle Slide Area");
        Transform handle = sliderObject.transform.Find("Handle Slide Area/Handle");
        if (handleArea != null && handle != null)
        {
            RectTransform areaRect = (RectTransform)handleArea;
            areaRect.offsetMin = new Vector2(24f, 0f);
            areaRect.offsetMax = new Vector2(-24f, 0f);
            ((RectTransform)handle).sizeDelta = new Vector2(52f, 24f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = SliderBall;
            handleImage.type = Image.Type.Simple;
            handleImage.preserveAspect = true;
            handleImage.color = Color.white;
        }

        RectTransform valueRect = NewText("Value", row, "-", 32f, TextAlignmentOptions.Left);
        valueRect.sizeDelta = new Vector2(120f, 60f);
        valueText = valueRect.GetComponent<TMP_Text>();

        return sliderObject.GetComponent<Slider>();
    }

    private static TMP_Text NewFullscreenValue(Transform button)
    {
        RectTransform value = NewText("UI_Valor", button, "SÍ", 40f, TextAlignmentOptions.Center);
        value.anchorMin = new Vector2(0.77f, 0.36f);
        value.anchorMax = new Vector2(0.92f, 0.64f);
        value.offsetMin = Vector2.zero;
        value.offsetMax = Vector2.zero;
        TMP_Text text = value.GetComponent<TMP_Text>();
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 40f;
        text.color = new Color(1f, 0.85f, 0.3f);
        return text;
    }
}
