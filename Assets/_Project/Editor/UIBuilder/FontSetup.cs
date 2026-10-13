using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIAssets;

public static class FontSetup
{
    internal static string FontFolder => UiRoot + "/Fonts";
    internal static string FontSourcePath => FontFolder + "/Bangers-Regular.ttf";
    internal static string FontAssetPath => FontFolder + "/Bangers SDF.asset";
    internal const string ExtraCharacters = "ÁÉÍÓÚÜÑáéíóúüñ¡¿·°%×";

    internal static TMP_FontAsset fontAsset;

    internal static void PrepareFontAsset()
    {
        AssetDatabase.ImportAsset(FontSourcePath);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        if (sourceFont == null)
            throw new InvalidOperationException($"No se encontró la fuente {FontSourcePath}.");

        fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            TMP_FontAsset previousDefault = TMP_Settings.defaultFontAsset;
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            fontAsset.name = "Bangers SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            fontAsset.atlasTextures[0].name = "Bangers SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            fontAsset.material.name = "Bangers SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            if (previousDefault != null && previousDefault != fontAsset)
                fontAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { previousDefault };
        }

        string basicCharacters = "";
        for (char c = ' '; c <= '~'; c++)
            basicCharacters += c;
        fontAsset.TryAddCharacters(basicCharacters + ExtraCharacters);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();

        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null)
        {
            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty defaultFont = serialized.FindProperty("m_defaultFontAsset");
            if (defaultFont != null)
            {
                defaultFont.objectReferenceValue = fontAsset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
        }
        else
        {
            Debug.LogWarning("[UI Builder] No se encontró 'TMP Settings'; los textos creados en ejecución usarán la fuente anterior.");
        }
    }

    internal static bool ApplyFont(GameObject root)
    {
        bool changed = false;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font == fontAsset)
                continue;

            text.font = fontAsset;
            text.fontSharedMaterial = fontAsset.material;
            EditorUtility.SetDirty(text);
            changed = true;
        }
        return changed;
    }

    internal static void ApplyFontToOpenScene()
    {
        if (fontAsset == null)
            return;

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            ApplyFont(root);
    }

    internal static void ApplyFontToPrefabs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (ApplyFont(root))
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
