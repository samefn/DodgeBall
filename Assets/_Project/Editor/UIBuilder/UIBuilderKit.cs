using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static UIAssets;
using static FontSetup;

public static class UIBuilderKit
{
    internal const string GeneratedPrefix = "UI_";
    internal static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    internal static readonly Color CardColor = new Color(0f, 0f, 0f, 0.65f);
    internal static readonly Color TextButtonColor = new Color(0.05f, 0.07f, 0.16f, 0.9f);

    internal static GameObject FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child.gameObject;
            }
        }
        return null;
    }

    internal static RectTransform RequireRect(string name)
    {
        GameObject found = FindInScene(name);
        if (found == null || !(found.transform is RectTransform rect))
            throw new InvalidOperationException($"No se encontró el objeto de UI '{name}' en la escena {SceneManager.GetActiveScene().name}.");
        return rect;
    }

    internal static void ResetGenerated(RectTransform container, params RectTransform[] originals)
    {
        foreach (RectTransform original in originals)
        {
            original.SetParent(container, false);
            original.gameObject.SetActive(true);
            original.localScale = Vector3.one;
            LayoutElement layout = original.GetComponent<LayoutElement>();
            if (layout != null)
                Object.DestroyImmediate(layout);
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child.name.StartsWith(GeneratedPrefix, StringComparison.Ordinal))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    internal static void SetupScaler(GameObject canvasObject)
    {
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    internal static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    internal static RectTransform NewPanel(string name, Transform parent)
    {
        RectTransform panel = NewUI(name, parent);
        Stretch(panel);
        return panel;
    }

    internal static void AddBackground(RectTransform parent, Sprite sprite, Color tint)
    {
        RectTransform background = NewUI("UI_Background", parent);
        background.SetAsFirstSibling();
        Stretch(background);
        Image image = background.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        image.raycastTarget = true;
        AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    internal static RectTransform NewCard(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float borderScale = 2.6f)
    {
        RectTransform card = NewUI(name, parent);
        Place(card, anchor, position, size);
        Image image = card.gameObject.AddComponent<Image>();
        image.raycastTarget = false;

        Sprite panel = TextPanel;
        if (panel != null)
        {
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = borderScale;
            image.color = Color.white;
        }
        else
        {
            image.color = CardColor;
        }
        return card;
    }

    internal static RectTransform NewColumn(string name, Transform parent, Vector2 anchor, Vector2 position, float spacing)
    {
        RectTransform column = NewUI(name, parent);
        Place(column, anchor, position, new Vector2(1000f, 700f));
        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return column;
    }

    internal static void NewImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, float height)
    {
        RectTransform rect = NewUI(name, parent);
        Place(rect, anchor, position, SizeForHeight(sprite, height));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    internal static Button NewImageButton(string name, Transform parent, Sprite sprite, float height, UnityAction onClick)
    {
        RectTransform rect = NewUI(name, parent);
        rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        StyleImageButton(rect.gameObject, sprite, height);
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    internal static void StyleImageButton(GameObject buttonObject, Sprite sprite, float height)
    {
        Image image = GetOrAdd<Image>(buttonObject);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = GetOrAdd<Button>(buttonObject);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors();
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        foreach (TMP_Text label in buttonObject.GetComponentsInChildren<TMP_Text>(true))
            label.gameObject.SetActive(false);
        foreach (Text label in buttonObject.GetComponentsInChildren<Text>(true))
            label.gameObject.SetActive(false);

        Vector2 size = SizeForHeight(sprite, height);
        ((RectTransform)buttonObject.transform).sizeDelta = size;
        AddLayoutSize(buttonObject, size);
        GetOrAdd<ButtonHoverScale>(buttonObject);
    }

    internal static Button NewTextButton(string name, Transform parent, string label, Vector2 size, UnityAction onClick)
    {
        RectTransform rect = NewUI(name, parent);
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = TextButtonColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = ButtonColors();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        rect.gameObject.AddComponent<ButtonHoverScale>();
        AddLayoutSize(rect.gameObject, size);

        RectTransform text = NewText("Label", rect, label, 28f, TextAlignmentOptions.Center);
        Stretch(text);
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    internal static RectTransform NewText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = NewUI(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
            text.font = fontAsset;
        else if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = content;
        StyleText(rect, fontSize, alignment, Color.white);
        return rect;
    }

    internal static void StyleText(RectTransform rect, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null)
            return;

        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
    }

    internal static ColorBlock ButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.93f, 0.8f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        colors.fadeDuration = 0.08f;
        return colors;
    }

    internal static void AddLayoutSize(GameObject target, Vector2 size)
    {
        LayoutElement layout = GetOrAdd<LayoutElement>(target);
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
    }

    internal static Vector2 SizeForHeight(Sprite sprite, float height)
    {
        return new Vector2(Mathf.Round(height * sprite.rect.width / sprite.rect.height), height);
    }

    internal static void MoveInto(RectTransform child, Transform parent)
    {
        child.SetParent(parent, false);
        child.SetAsLastSibling();
    }

    internal static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    internal static void StretchWithPadding(RectTransform rect, float padding)
    {
        Stretch(rect);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    internal static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    internal static void PlaceKeepSize(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        Place(rect, anchor, position, rect.sizeDelta);
    }

    internal static T GetOrAdd<T>(GameObject target) where T : Component
    {
        return target.TryGetComponent(out T component) ? component : target.AddComponent<T>();
    }

    internal static void SetLayerRecursively(GameObject target, int layer)
    {
        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    internal static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    internal static RectTransform RequireChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name && child is RectTransform rect)
                return rect;
        }
        throw new InvalidOperationException($"No se encontró '{name}' en el HUD del jugador.");
    }

    internal static void SetReferences(Object target, params (string field, Object value)[] references)
    {
        SerializedObject serialized = new SerializedObject(target);
        foreach ((string field, Object value) in references)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo '{field}'.");
            property.objectReferenceValue = value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
