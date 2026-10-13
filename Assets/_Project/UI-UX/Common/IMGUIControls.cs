using UnityEngine;

public static class IMGUIControls
{
    private const float ThumbSize = 28f;
    private const int TrackHeight = 28;

    private static GUIStyle richLabel;
    private static GUIStyle richButton;
    private static GUIStyle sliderStyle;
    private static GUIStyle thumbStyle;

    private static UISkin Skin => UISkin.Instance;

    public static GUIStyle RichLabel()
    {
        if (richLabel == null)
            richLabel = new GUIStyle(GUI.skin.label) { richText = true };
        return richLabel;
    }

    public static GUIStyle RichButton()
    {
        if (richButton == null)
            richButton = new GUIStyle(GUI.skin.button) { richText = true };
        return richButton;
    }

    public static bool TextButton(string label, GUIStyle style, params GUILayoutOption[] options)
    {
        bool pressed = GUILayout.Button(label, style, options);
        if (pressed)
            UIAudio.PlayClick();
        return pressed;
    }

    public static bool ImageButton(Sprite sprite, string fallback, float height)
    {
        if (sprite == null)
            return TextButton(fallback, GUI.skin.button, GUILayout.Height(height));

        float width = Mathf.Round(height * sprite.rect.width / sprite.rect.height);
        Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        bool hovered = rect.Contains(Event.current.mousePosition);

        if (Event.current.type == EventType.Repaint)
        {
            Texture2D texture = sprite.texture;
            Rect source = sprite.textureRect;
            Rect uv = new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height);
            Rect drawRect = hovered ? new Rect(rect.x - width * 0.03f, rect.y - height * 0.03f, width * 1.06f, height * 1.06f) : rect;

            Color previous = GUI.color;
            GUI.color = hovered ? new Color(1f, 0.93f, 0.8f) : Color.white;
            GUI.DrawTextureWithTexCoords(drawRect, texture, uv);
            GUI.color = previous;
        }

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        if (clicked)
            UIAudio.PlayClick();
        return clicked;
    }

    public static float BallSlider(string label, float value, float min, float max)
    {
        GUILayout.Label(label);
        EnsureSliderStyles();
        if (thumbStyle == null)
            return GUILayout.HorizontalSlider(value, min, max);

        return GUILayout.HorizontalSlider(value, min, max, sliderStyle, thumbStyle, GUILayout.Height(ThumbSize));
    }

    private static void EnsureSliderStyles()
    {
        if (thumbStyle != null || Skin == null || Skin.sliderThumb == null)
            return;

        sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
        {
            fixedHeight = ThumbSize,
            margin = new RectOffset(6, 6, 4, 8),
            border = new RectOffset(3, 3, 0, 0),
            padding = new RectOffset(0, 0, 0, 0)
        };
        SetBackground(sliderStyle, CreateTrackTexture());

        thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
        {
            fixedWidth = ThumbSize,
            fixedHeight = ThumbSize,
            border = new RectOffset(0, 0, 0, 0),
            overflow = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        SetBackground(thumbStyle, Skin.sliderThumb.texture);
    }

    private static void SetBackground(GUIStyle style, Texture2D background)
    {
        style.normal.background = background;
        style.hover.background = background;
        style.active.background = background;
        style.focused.background = background;
    }

    private static Texture2D CreateTrackTexture()
    {
        Texture2D texture = new Texture2D(8, TrackHeight, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        Color line = new Color(0.85f, 0.15f, 0.15f, 1f);
        Color edge = new Color(0f, 0f, 0f, 0.8f);
        for (int row = 0; row < TrackHeight; row++)
        {
            int distance = Mathf.Abs(row - TrackHeight / 2);
            Color color = distance <= 2 ? line : distance == 3 ? edge : Color.clear;
            for (int column = 0; column < texture.width; column++)
                texture.SetPixel(column, row, color);
        }
        texture.Apply();
        return texture;
    }
}
