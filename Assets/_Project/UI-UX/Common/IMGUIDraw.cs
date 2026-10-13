using UnityEngine;

public static class IMGUIDraw
{
    public static void Sliced(Rect rect, Sprite sprite, float borderSize, Color tint)
    {
        if (sprite == null || Event.current.type != EventType.Repaint)
            return;

        Texture2D texture = sprite.texture;
        Rect source = sprite.textureRect;
        Vector4 border = sprite.border;
        if (border == Vector4.zero)
            border = Vector4.one * Mathf.Min(source.width, source.height) * 0.14f;

        float drawBorder = Mathf.Min(borderSize, rect.width * 0.5f, rect.height * 0.5f);
        float[] xs = { rect.x, rect.x + drawBorder, rect.xMax - drawBorder, rect.xMax };
        float[] ys = { rect.y, rect.y + drawBorder, rect.yMax - drawBorder, rect.yMax };

        float[] us =
        {
            source.xMin / texture.width,
            (source.xMin + border.x) / texture.width,
            (source.xMax - border.z) / texture.width,
            source.xMax / texture.width
        };
        float[] vs =
        {
            source.yMax / texture.height,
            (source.yMax - border.w) / texture.height,
            (source.yMin + border.y) / texture.height,
            source.yMin / texture.height
        };

        Color previous = GUI.color;
        GUI.color = tint;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Rect target = Rect.MinMaxRect(xs[column], ys[row], xs[column + 1], ys[row + 1]);
                Rect uv = Rect.MinMaxRect(us[column], vs[row + 1], us[column + 1], vs[row]);
                if (target.width > 0f && target.height > 0f)
                    GUI.DrawTextureWithTexCoords(target, texture, uv);
            }
        }
        GUI.color = previous;
    }

    public static void Panel(Rect rect)
    {
        UISkin skin = UISkin.Instance;
        if (skin != null && skin.textPanel != null)
        {
            Sliced(rect, skin.textPanel, 34f, Color.white);
            return;
        }

        GUI.Box(rect, GUIContent.none);
        GUI.Box(rect, GUIContent.none);
    }
}
