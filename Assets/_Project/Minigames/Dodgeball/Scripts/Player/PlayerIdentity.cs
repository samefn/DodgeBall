using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    private static readonly Color[] Palette =
    {
        new Color(0.95f, 0.26f, 0.21f),
        new Color(0.13f, 0.59f, 0.95f),
        new Color(0.30f, 0.80f, 0.31f),
        new Color(1.00f, 0.76f, 0.03f),
        new Color(0.61f, 0.15f, 0.69f),
        new Color(1.00f, 0.50f, 0.00f)
    };

    private static readonly string[] ColorNames = { "Rojo", "Azul", "Verde", "Amarillo", "Morado", "Naranja" };

    public int PlayerId { get; private set; }
    public Color Color => ColorFor(PlayerId);
    public string DisplayName => DisplayNameFor(PlayerId);

    public void Assign(int playerId)
    {
        PlayerId = playerId;
        gameObject.name = $"Player_{playerId:00}";
    }

    public static Color ColorFor(int playerId)
    {
        return Palette[PaletteIndex(playerId)];
    }

    public static string ColorNameFor(int playerId)
    {
        return ColorNames[PaletteIndex(playerId)];
    }

    public static string DisplayNameFor(int playerId)
    {
        return $"Jugador {playerId}";
    }

    private static int PaletteIndex(int playerId)
    {
        return (Mathf.Max(playerId, 1) - 1) % Palette.Length;
    }
}
