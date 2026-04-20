using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class LoopfallMusic : MonoBehaviour
{
    private const string MenuSceneName = "StartMenu";
    private const string GameplaySceneName = "Loopfall";

    private const string MenuMusicResourcePath = "Audio/the_mountain-ambient-piano-132384";
    private const string MenuMusicEditorAssetPath = "Assets/Resources/Audio/the_mountain-ambient-piano-132384.mp3";
    private const string MenuMusicLabel = "the_mountain-ambient-piano-132384.mp3";

    private const string GameplayMusicResourcePath = "Audio/the_mountain-ambient-487008";
    private const string GameplayMusicEditorAssetPath = "Assets/Resources/Audio/the_mountain-ambient-487008.mp3";
    private const string GameplayMusicLabel = "the_mountain-ambient-487008.mp3";

    private const float MenuVolume = 0.6f;
    private const float GameplayVolume = 0.46f;

    private enum MusicContext
    {
        None,
        Menu,
        Gameplay,
    }

    public static LoopfallMusic Instance { get; private set; } = null!;

    private AudioSource menuSource = null!;
    private AudioSource gameplaySource = null!;
    private AudioClip menuClip = null!;
    private AudioClip gameplayClip = null!;
    private MusicContext activeContext = MusicContext.None;
    private bool initialized;
    private float menuTargetVolume = MenuVolume;
    private float menuTargetPitch = 1f;

    public static LoopfallMusic EnsureExists()
    {
        if (Instance != null)
        {
            Instance.Initialize();
            return Instance;
        }

        LoopfallMusic existing = FindFirstObjectByType<LoopfallMusic>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            existing.Initialize();
            return existing;
        }

        GameObject musicObject = new("LoopfallMusic");
        Instance = musicObject.AddComponent<LoopfallMusic>();
        DontDestroyOnLoad(musicObject);
        Instance.Initialize();
        return Instance;
    }

    public void PlayMenuMusic(bool restart = false)
    {
        Initialize();
        PlayExclusive(MusicContext.Menu, restart);
    }

    public void PlayGameplayMusic(bool restart = false)
    {
        Initialize();
        PlayExclusive(MusicContext.Gameplay, restart);
    }

    public void RouteForActiveScene(bool restart = false)
    {
        RouteForScene(SceneManager.GetActiveScene().name, restart);
    }

    public void RouteForScene(string sceneName, bool restart = false)
    {
        if (sceneName == MenuSceneName)
        {
            PlayMenuMusic(restart);
            return;
        }

        if (sceneName == GameplaySceneName)
        {
            PlayGameplayMusic(restart);
            return;
        }

        StopMusicAbruptly();
    }

    public void SetMenuIntensity(bool inGlitch, bool crashing, bool transitioning)
    {
        Initialize();

        if (activeContext != MusicContext.Menu)
        {
            if (!crashing && SceneManager.GetActiveScene().name == MenuSceneName)
            {
                PlayMenuMusic(false);
            }

            return;
        }

        menuTargetVolume = crashing ? 0.1f : transitioning ? 0.26f : inGlitch ? 0.42f : MenuVolume;
        menuTargetPitch = crashing ? 0.92f : transitioning ? 0.96f : inGlitch ? 0.98f : 1f;
        ApplyMenuTargets();
    }

    public void StopMusicAbruptly()
    {
        Initialize();

        StopExternalMusicClipSources();
        StopAndSilence(menuSource);
        StopAndSilence(gameplaySource);
        activeContext = MusicContext.None;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null!;
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (activeContext == MusicContext.Menu)
        {
            if (gameplaySource != null && gameplaySource.isPlaying)
            {
                gameplaySource.Stop();
            }

            if (menuSource != null && menuSource.clip == menuClip && !menuSource.isPlaying)
            {
                menuSource.Play();
            }

            ApplyMenuTargets();
        }
        else if (activeContext == MusicContext.Gameplay)
        {
            if (menuSource != null && menuSource.isPlaying)
            {
                menuSource.Stop();
            }

            if (gameplaySource != null && gameplaySource.clip == gameplayClip && !gameplaySource.isPlaying)
            {
                gameplaySource.Play();
            }

            if (gameplaySource != null)
            {
                gameplaySource.volume = Mathf.Lerp(gameplaySource.volume, GameplayVolume, 5f * Time.unscaledDeltaTime);
                gameplaySource.pitch = Mathf.Lerp(gameplaySource.pitch, 1f, 4f * Time.unscaledDeltaTime);
            }
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        menuSource = CreateChildSource("Menu Music");
        gameplaySource = CreateChildSource("Gameplay Music");
        LoadMusicClips();
        ValidateMusicAssignments();
        initialized = true;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        RouteForScene(scene.name, true);
    }

    private void PlayExclusive(MusicContext context, bool restart)
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        AudioSource targetSource = context == MusicContext.Menu ? menuSource : gameplaySource;
        AudioSource otherSource = context == MusicContext.Menu ? gameplaySource : menuSource;
        AudioClip targetClip = context == MusicContext.Menu ? menuClip : gameplayClip;
        string targetLabel = context == MusicContext.Menu ? MenuMusicLabel : GameplayMusicLabel;

        StopAndSilence(otherSource);
        StopExternalMusicClipSources();

        activeContext = context;
        menuTargetVolume = context == MusicContext.Menu ? MenuVolume : menuTargetVolume;
        menuTargetPitch = context == MusicContext.Menu ? 1f : menuTargetPitch;

        if (targetClip == null)
        {
            Debug.LogError($"LoopfallMusic could not load {(context == MusicContext.Menu ? "menu" : "gameplay")} music '{targetLabel}'. Check Assets/Resources/Audio.");
            StopAndSilence(targetSource);
            return;
        }

        ConfigureMusicSource(targetSource);

        bool alreadyCorrectTrack = targetSource.clip == targetClip && targetSource.isPlaying;
        if (!alreadyCorrectTrack || restart)
        {
            targetSource.Stop();
            targetSource.clip = targetClip;
            targetSource.volume = context == MusicContext.Menu ? MenuVolume : GameplayVolume;
            targetSource.pitch = 1f;
            targetSource.Play();
        }

        Debug.Log($"LoopfallMusic routed {(context == MusicContext.Menu ? "MENU" : "GAMEPLAY")} music to '{targetLabel}'. The other music source is stopped.");
    }

    private void LoadMusicClips()
    {
        menuClip ??= LoadMusicClip(MenuMusicResourcePath, MenuMusicEditorAssetPath);
        gameplayClip ??= LoadMusicClip(GameplayMusicResourcePath, GameplayMusicEditorAssetPath);
    }

    private void ValidateMusicAssignments()
    {
        if (string.Equals(MenuMusicResourcePath, GameplayMusicResourcePath, StringComparison.Ordinal))
        {
            Debug.LogError("LoopfallMusic has the same resource path for menu and gameplay music. They must be separate tracks.");
        }

        if (menuClip != null && gameplayClip != null && ReferenceEquals(menuClip, gameplayClip))
        {
            Debug.LogError("LoopfallMusic resolved the same AudioClip for menu and gameplay music. Check the music file assignments.");
        }
    }

    private AudioClip LoadMusicClip(string resourcePath, string editorAssetPath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);

#if UNITY_EDITOR
        if (clip == null)
        {
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(editorAssetPath);
        }
#endif

        return clip;
    }

    private void StopExternalMusicClipSources()
    {
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source == null || source == menuSource || source == gameplaySource || source.transform.IsChildOf(transform))
            {
                continue;
            }

            if (source.clip == menuClip || source.clip == gameplayClip)
            {
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }
        }
    }

    private void ApplyMenuTargets()
    {
        if (menuSource == null)
        {
            return;
        }

        menuSource.volume = Mathf.Lerp(menuSource.volume, menuTargetVolume, 6f * Time.unscaledDeltaTime);
        menuSource.pitch = Mathf.Lerp(menuSource.pitch, menuTargetPitch, 5f * Time.unscaledDeltaTime);
    }

    private AudioSource CreateChildSource(string name)
    {
        Transform existingChild = transform.Find(name);
        GameObject child = existingChild != null ? existingChild.gameObject : new GameObject(name);
        child.transform.SetParent(transform, false);

        AudioSource source = child.GetComponent<AudioSource>();
        if (source == null)
        {
            source = child.AddComponent<AudioSource>();
        }

        ConfigureMusicSource(source);
        return source;
    }

    private void ConfigureMusicSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.minDistance = 2f;
        source.maxDistance = 30f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.ignoreListenerPause = true;
        source.mute = false;
        source.enabled = true;
        source.priority = 32;
    }

    private void StopAndSilence(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        source.volume = 0f;
        source.pitch = 1f;
    }
}
