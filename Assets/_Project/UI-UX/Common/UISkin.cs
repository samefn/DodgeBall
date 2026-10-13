using UnityEngine;

[CreateAssetMenu(menuName = "Dodgeball/UI Skin", fileName = "UISkin")]
public class UISkin : ScriptableObject
{
    public const string ResourcePath = "UISkin";

    private static UISkin instance;
    private static bool searched;

    [Tooltip("Framed panel for text-heavy boxes. Drawn 9-sliced using the sprite borders.")]
    public Sprite textPanel;
    public Font font;
    public Sprite kickButton;
    public Sprite banButton;
    public Sprite pauseButton;
    public Sprite applyRulesButton;
    public Sprite defaultRulesButton;
    public Sprite restartButton;
    public Sprite endNowButton;
    public Sprite closePanelButton;

    [Header("Results")]
    public Sprite victoryBanner;
    public Sprite defeatBanner;

    [Header("Sliders")]
    [Tooltip("Ball used as the handle of the sliders (host panel and settings).")]
    public Sprite sliderThumb;

    [Header("Sounds")]
    public AudioClip clickSound;
    public AudioClip lifeLostSound;
    public AudioClip throwSound;
    public AudioClip impactSound;
    public AudioClip victorySound;
    public AudioClip defeatSound;
    [Tooltip("Looping music of the main menu and the lobby.")]
    public AudioClip menuMusic;

    public static UISkin Instance
    {
        get
        {
            if (!searched)
            {
                instance = Resources.Load<UISkin>(ResourcePath);
                searched = true;
            }
            return instance;
        }
    }

    public static void ApplyIMGUIFont()
    {
        UISkin skin = Instance;
        if (skin != null && skin.font != null && GUI.skin.font != skin.font)
            GUI.skin.font = skin.font;
    }
}
