using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static FontSetup;
using static UIAssets;

public static class DodgeballUIBuilder
{
    private const string SceneDir = "Assets/_Project/Minigames/Dodgeball/Scenes/";

    [MenuItem("Dodgeball/Aplicar diseño de UI a las escenas")]
    public static void ApplyToAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string previousScene = SceneManager.GetActiveScene().path;
        try
        {
            PrepareTextPanelSprite();
            PrepareFontAsset();
            BuildScene("MainMenu", MainMenuLayout.Build);
            BuildScene("Lobby", LobbyLayout.Build);
            BuildScene("Dodgeball_Arena", ArenaLayout.Build);
            BuildScene("Bootstrap", () => { });
            PrepareSliderSprite();
            PlayerHudLayout.BuildAll();
            ApplyFontToPrefabs();
            UISkinAssetBuilder.Build();
            Debug.Log("[UI Builder] Diseño y tipografía aplicados a MainMenu, Lobby, Dodgeball_Arena y los prefabs.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Dodgeball UI", $"No se pudo aplicar el diseño:\n{e.Message}", "OK");
        }

        if (!string.IsNullOrEmpty(previousScene))
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
    }

    private static void BuildScene(string sceneName, Action build)
    {
        Scene scene = EditorSceneManager.OpenScene(SceneDir + sceneName + ".unity", OpenSceneMode.Single);
        build();
        ApplyFontToOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
