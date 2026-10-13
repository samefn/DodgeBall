using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIAudio : MonoBehaviour
{
    private const float MusicVolume = 0.45f;
    private const float WorldMinDistance = 3f;
    private const float WorldMaxDistance = 45f;

    private static UIAudio instance;

    private AudioSource source;
    private AudioSource music;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject(nameof(UIAudio));
        DontDestroyOnLoad(root);
        instance = root.AddComponent<UIAudio>();
        instance.source = CreateSource(root, false);
        instance.music = CreateSource(root, true);
        instance.music.volume = MusicVolume;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static AudioSource CreateSource(GameObject root, bool loop)
    {
        AudioSource audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private static UISkin Skin => UISkin.Instance;

    public static void PlayClick() => Play(Skin != null ? Skin.clickSound : null, 0.8f);
    public static void PlayLifeLost() => Play(Skin != null ? Skin.lifeLostSound : null, 1f);
    public static void PlayVictory() => Play(Skin != null ? Skin.victorySound : null, 1f);
    public static void PlayDefeat() => Play(Skin != null ? Skin.defeatSound : null, 1f);

    public static void PlayThrowAt(Vector3 position) => PlayAt(Skin != null ? Skin.throwSound : null, position, 0.9f);
    public static void PlayImpactAt(Vector3 position) => PlayAt(Skin != null ? Skin.impactSound : null, position, 1f);

    private static void Play(AudioClip clip, float volume)
    {
        if (clip == null || instance == null || instance.source == null)
            return;

        instance.source.PlayOneShot(clip, volume);
    }

    private static void PlayAt(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
            return;

        GameObject temporary = new GameObject("WorldSound");
        temporary.transform.position = position;
        AudioSource audioSource = temporary.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = WorldMinDistance;
        audioSource.maxDistance = WorldMaxDistance;
        audioSource.Play();
        Destroy(temporary, clip.length + 0.1f);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookButtons(scene);
        UpdateMusic(scene.name);
    }

    private static void HookButtons(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveListener(PlayClick);
                button.onClick.AddListener(PlayClick);
            }
        }
    }

    private static void UpdateMusic(string sceneName)
    {
        if (instance == null || instance.music == null)
            return;

        bool menuScene = sceneName == SceneNames.MainMenu || sceneName == SceneNames.Lobby;
        AudioClip clip = Skin != null ? Skin.menuMusic : null;
        if (!menuScene || clip == null)
        {
            instance.music.Stop();
            return;
        }

        if (instance.music.isPlaying && instance.music.clip == clip)
            return;

        instance.music.clip = clip;
        instance.music.Play();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }
}
