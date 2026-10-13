using UnityEditor;
using UnityEngine;
using static FontSetup;
using static UIAssets;

public static class UISkinAssetBuilder
{
    private static string SkinFolder => UiRoot + "/Resources";
    private static string SkinPath => SkinFolder + "/" + UISkin.ResourcePath + ".asset";

    internal static void Build()
    {
        if (!AssetDatabase.IsValidFolder(SkinFolder))
            AssetDatabase.CreateFolder(UiRoot, "Resources");

        UISkin skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<UISkin>();
            AssetDatabase.CreateAsset(skin, SkinPath);
        }

        skin.kickButton = KickButton;
        skin.banButton = BanButton;
        skin.pauseButton = PauseMatchButton;
        skin.applyRulesButton = ApplyRulesButton;
        skin.defaultRulesButton = DefaultRulesButton;
        skin.textPanel = TextPanel;
        skin.font = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        skin.sliderThumb = SliderBall;
        skin.clickSound = LoadSound("clic_boton.wav");
        skin.lifeLostSound = LoadSound("menos_una_vida.wav");
        skin.throwSound = LoadSound("lanzamiento.wav");
        skin.impactSound = LoadSound("impacto_personaje.wav");
        skin.victorySound = LoadSound("victoria_estadio.wav");
        skin.defeatSound = LoadSound("derrota.wav");
        skin.menuMusic = LoadSound("lobby_menu_loop.wav");
        skin.restartButton = RestartButton;
        skin.endNowButton = EndNowButton;
        skin.closePanelButton = ClosePanelButton;
        skin.victoryBanner = VictoryBanner;
        skin.defeatBanner = DefeatBanner;
        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
    }
}
