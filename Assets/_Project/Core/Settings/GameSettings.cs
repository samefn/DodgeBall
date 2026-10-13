using UnityEngine;

public static class GameSettings
{
    public const float MinSensitivity = 0.2f;
    public const float MaxSensitivity = 3f;

    private const string SensitivityKey = "Dodgeball.MouseSensitivity";
    private const string VolumeKey = "Dodgeball.Volume";

    private static float? sensitivity;

    public static float MouseSensitivity
    {
        get
        {
            if (!sensitivity.HasValue)
                sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
            return sensitivity.Value;
        }
        set
        {
            sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(SensitivityKey, sensitivity.Value);
        }
    }

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, volume);
            AudioListener.volume = volume;
        }
    }

    public static bool Fullscreen
    {
        get => Screen.fullScreen;
        set => Screen.fullScreen = value;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySaved()
    {
        sensitivity = null;
        AudioListener.volume = Volume;
    }
}
