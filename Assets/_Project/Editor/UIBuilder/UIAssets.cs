using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIAssets
{
    internal static string UiRoot
    {
        get
        {
            foreach (string guid in AssetDatabase.FindAssets("UISkin t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                const string suffix = "/Common/UISkin.cs";
                if (path.EndsWith(suffix, StringComparison.Ordinal))
                    return path.Substring(0, path.Length - suffix.Length);
            }
            throw new InvalidOperationException("No se encontró la carpeta de UI (Common/UISkin.cs).");
        }
    }
    internal static string ImageDir => UiRoot + "/Images-Buttons/";

    internal static Sprite MainMenuBackground => LoadSprite("MainMenu-BG.jpeg");
    internal static Sprite RoomsBackground => LoadSprite("Pause-Rooms-BG.jpeg");
    internal static Sprite Title => LoadSprite("TITULO-Image.png");
    internal static Sprite PlayButton => LoadSprite("JUGAR-Button.png");
    internal static Sprite PlayLocalButton => LoadSprite("JUGAR LOCAL-Button.png");
    internal static Sprite SettingsButton => LoadSprite("Configuracion-Button.png");
    internal static Sprite PauseSettingsButton => LoadSprite("ConfigPauseMenu-Button.png");
    internal static Sprite ExitButton => LoadSprite("SALIR-Button.png");
    internal static Sprite QuitGameButton => LoadSprite("SALIR DEL JUEGO-Button.png");
    internal static Sprite BackToMenuButton => LoadSprite("VOLVER AL MENU-Button.png");
    internal static Sprite ContinueButton => LoadSprite("CONTINUAR-Button.png");
    internal static Sprite CreateRoomButton => LoadSprite("crear sala-Button.png");
    internal static Sprite JoinRoomButton => LoadSprite("unirse a la sala-Button.png");
    internal static Sprite RoomNameInput => LoadSprite("nombre de la sala-Input.png");
    internal static Sprite KickButton => LoadSprite("EXPULSAR-Button.png");
    internal static Sprite BanButton => LoadSprite("BANNEAR-Button.png");
    internal static Sprite PauseMatchButton => LoadSprite("PAUSAR-Button.png");
    internal static Sprite RematchButton => LoadSprite("REVANCHA-Button.png");
    internal static Sprite ApplyRulesButton => LoadSprite("APLICAR REGLAS-Button.png");
    internal static Sprite DefaultRulesButton => LoadSprite("VALORES POR DEFECTO-Button.png");
    internal static Sprite LivesHud => LoadSprite("vidas-UI.png");
    internal static Sprite TimeHud => LoadSprite("tiempo-UI.png");
    internal static Sprite ScoreHud => LoadSprite("puntos-UI.png");
    internal static Sprite SliderBall => LoadSprite("Balon-slider.png");
    internal static Sprite BallHud => LoadSprite("PELOTA-Hud.png");
    internal static Sprite CloseButton => LoadSprite("CERRAR.png");
    internal static Sprite ClosePanelButton => LoadSprite("CERRAR PANEL-Button.png");
    internal static Sprite HostPanelButton => LoadSprite("PANEL HOST.png");
    internal static Sprite RestartButton => LoadSprite("REINICIAR.png");
    internal static Sprite EndNowButton => LoadSprite("TERMINAR AHORA-Button.png");
    internal static Sprite VictoryBanner => LoadSprite("VICTORIA-Panel.png");
    internal static Sprite DefeatBanner => LoadSprite("DERROTA-Panel.png");
    internal static Sprite ChargeBarSprite => LoadSprite("ChargeBar-Slider.png");
    internal static Sprite FullscreenButton => LoadSprite("PantallaCompleta-Button.png");
    internal static Sprite RulesPanel => LoadSprite("Reglas-Panel.png");

    internal static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImageDir + fileName);
        if (sprite == null)
            throw new InvalidOperationException($"No se encontró el sprite {ImageDir}{fileName} (Texture Type debe ser 'Sprite (2D and UI)').");
        return sprite;
    }

    internal static AudioClip LoadSound(string fileName)
    {
        string path = UiRoot + "/Sounds/" + fileName;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            Debug.LogWarning($"[UI Builder] No se encontró el sonido {path}.");
        return clip;
    }

    internal static string TextPanelPath => ImageDir + "panel-BG.png";
    internal static readonly Vector4 TextPanelBorder = new Vector4(130f, 130f, 130f, 130f);
    internal static Sprite TextPanel => AssetDatabase.LoadAssetAtPath<Sprite>(TextPanelPath);

    internal static void PrepareTextPanelSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TextPanelPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[UI Builder] No se encontró {TextPanelPath}; los paneles de texto usarán un fondo oscuro.");
            return;
        }

        if (importer.textureType == TextureImporterType.Sprite && importer.spriteBorder == TextPanelBorder)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = TextPanelBorder;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    internal static void PrepareSliderSprite()
    {
        string path = ImageDir + "Balon-slider.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || (importer.mipmapEnabled && importer.maxTextureSize == 256))
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 256;
        importer.SaveAndReimport();
    }
}
