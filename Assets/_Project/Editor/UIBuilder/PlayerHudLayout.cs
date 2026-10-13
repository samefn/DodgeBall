using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIAssets;
using static UIBuilderKit;

public static class PlayerHudLayout
{
    internal static void BuildAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PlayerHUD hud = root.GetComponentInChildren<PlayerHUD>(true);
                if (hud == null)
                    continue;

                BuildPlayerHud(hud);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void BuildPlayerHud(PlayerHUD hud)
    {
        Canvas canvas = hud.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            throw new InvalidOperationException("El HUD del jugador no tiene Canvas.");

        RectTransform container = (RectTransform)canvas.transform;
        SetupScaler(canvas.gameObject);
        int layer = canvas.gameObject.layer;

        RectTransform lives = RequireChild(container, "LivesText");
        RectTransform timer = RequireChild(container, "TimerText");
        RectTransform score = RequireChild(container, "ScoreText");
        RectTransform ball = RequireChild(container, "BallText");
        ResetGenerated(container, lives, timer, score, ball);

        RectTransform livesBox = NewHudBox("UI_VidasBox", container, LivesHud, new Vector2(0f, 1f), new Vector2(150f, -85f), lives);
        NewHudBox("UI_TiempoBox", container, TimeHud, new Vector2(0.5f, 1f), new Vector2(0f, -85f), timer);
        NewHudBox("UI_PuntosBox", container, ScoreHud, new Vector2(1f, 1f), new Vector2(-150f, -85f), score);
        RectTransform ballBox = NewHudBox("UI_PelotaBox", container, BallHud, new Vector2(0f, 0f), new Vector2(190f, 75f), ball, 95f, 0.6f);
        GameObject ballIcon = NewBallIcon(ballBox);
        StyleChargeBar(container);
        foreach (string boxName in new[] { "UI_VidasBox", "UI_TiempoBox", "UI_PuntosBox", "UI_PelotaBox" })
            SetLayerRecursively(container.Find(boxName).gameObject, layer);

        SerializedObject serialized = new SerializedObject(hud);
        serialized.FindProperty("compactLabels").boolValue = true;
        serialized.FindProperty("livesBox").objectReferenceValue = livesBox;
        serialized.FindProperty("ballIcon").objectReferenceValue = ballIcon;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform NewHudBox(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, RectTransform valueText, float height = 120f, float textStart = 0.4f)
    {
        Vector2 size = SizeForHeight(sprite, height);
        RectTransform box = NewUI(name, parent);
        Place(box, anchor, position, size);
        Image image = box.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        MoveInto(valueText, box);
        valueText.anchorMin = Vector2.zero;
        valueText.anchorMax = Vector2.one;
        valueText.pivot = new Vector2(0.5f, 0.5f);
        valueText.offsetMin = new Vector2(size.x * textStart, size.y * 0.22f);
        valueText.offsetMax = new Vector2(-size.x * 0.07f, -size.y * 0.22f);

        TMP_Text text = valueText.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 58f;
            text.raycastTarget = false;
        }
        return box;
    }

    private static GameObject NewBallIcon(RectTransform ballBox)
    {
        Vector2 size = ballBox.sizeDelta;
        RectTransform icon = NewUI("UI_BallIcon", ballBox);
        icon.anchorMin = Vector2.zero;
        icon.anchorMax = Vector2.one;
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.offsetMin = new Vector2(size.x * 0.765f, size.y * 0.26f);
        icon.offsetMax = new Vector2(-size.x * 0.1f, -size.y * 0.26f);

        Image image = icon.gameObject.AddComponent<Image>();
        image.sprite = SliderBall;
        image.preserveAspect = true;
        image.raycastTarget = false;
        icon.gameObject.SetActive(false);
        return icon.gameObject;
    }

    private static void StyleChargeBar(RectTransform container)
    {
        Transform barTransform = FindChild(container, "ChargeBar");
        Transform fillTransform = FindChild(container, "ChargeFill");
        if (barTransform == null || fillTransform == null)
        {
            Debug.LogWarning("[UI Builder] El HUD no tiene ChargeBar/ChargeFill; la barra de carga no se cambió.");
            return;
        }

        RectTransform bar = (RectTransform)barTransform;
        Place(bar, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), SizeForHeight(ChargeBarSprite, 60f));
        Image background = GetOrAdd<Image>(bar.gameObject);
        background.sprite = ChargeBarSprite;
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
        background.color = new Color(0.25f, 0.25f, 0.25f, 0.65f);
        background.raycastTarget = false;

        RectTransform fill = (RectTransform)fillTransform;
        Stretch(fill);
        Image fillImage = GetOrAdd<Image>(fill.gameObject);
        fillImage.sprite = ChargeBarSprite;
        fillImage.color = Color.white;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.preserveAspect = false;
        fillImage.raycastTarget = false;
    }
}
