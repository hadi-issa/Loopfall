using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LoopfallCue
{
    ButtonHover,
    ButtonClick,
    MenuDrone,
    MenuWarmPad,
    MenuStatic,
    MenuGlitchTick,
    MenuGlitchBurst,
    CrashSting,
    WorldWind,
    WorldTone,
    FountainLoop,
    ShrineLoop,
    FragmentLoop,
    FragmentPickupGood,
    FragmentPickupBad,
    ShrineAccept,
    ShrineReject,
    DecayWarning,
    DecayCollapse,
    RollLoop,
}

public class LoopfallAudio : MonoBehaviour
{
    private const int SampleRate = 22050;

    public static LoopfallAudio Instance { get; private set; } = null!;

    private readonly Dictionary<LoopfallCue, AudioClip> clipCache = new();

    private AudioSource uiSource = null!;
    private float nextMenuArtifactTime;
    private bool crashFlickerActive;
    private float crashFlickerUntil;
    private float nextCrashFlickerTime;
    private bool startTransitionActive;
    private float startTransitionUntil;

    public static LoopfallAudio EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LoopfallAudio existing = FindFirstObjectByType<LoopfallAudio>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject audioObject = new("LoopfallAudio");
        Instance = audioObject.AddComponent<LoopfallAudio>();
        DontDestroyOnLoad(audioObject);
        return Instance;
    }

    public AudioClip GetClip(LoopfallCue cue)
    {
        if (clipCache.TryGetValue(cue, out AudioClip existing))
        {
            return existing;
        }

        AudioClip clip = BuildClip(cue);
        clipCache[cue] = clip;
        return clip;
    }

    public void PlayUi(LoopfallCue cue, float volume = 1f, float pitchVariance = 0.06f)
    {
        if (uiSource == null)
        {
            return;
        }

        uiSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
        uiSource.PlayOneShot(GetClip(cue), Mathf.Clamp01(volume));
    }

    public void PlayAt(LoopfallCue cue, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f, float maxDistance = 24f)
    {
        AudioClip clip = GetClip(cue);
        GameObject emitterObject = new($"{cue} Audio");
        emitterObject.transform.position = position;

        AudioSource source = emitterObject.AddComponent<AudioSource>();
        ConfigureBaseSource(source);
        source.clip = clip;
        source.loop = false;
        source.pitch = pitch;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.maxDistance = maxDistance;
        source.minDistance = 2f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        Destroy(emitterObject, clip.length / Mathf.Max(0.05f, pitch) + 0.2f);
    }

    public void ConfigureLoopSource(AudioSource source, LoopfallCue cue, float volume, float pitch, float spatialBlend, float minDistance = 2f, float maxDistance = 30f)
    {
        if (source == null)
        {
            return;
        }

        ConfigureBaseSource(source);
        source.clip = GetClip(cue);
        source.loop = true;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
    }

    public void SetMenuState(bool inGlitch, bool crashing)
    {
        LoopfallMusic.EnsureExists().SetMenuIntensity(inGlitch, crashing, startTransitionActive);

        if (inGlitch && Time.unscaledTime >= nextMenuArtifactTime)
        {
            PlayUi(UnityEngine.Random.value > 0.72f ? LoopfallCue.MenuGlitchBurst : LoopfallCue.MenuGlitchTick, UnityEngine.Random.Range(0.1f, 0.2f), 0.12f);
            nextMenuArtifactTime = Time.unscaledTime + UnityEngine.Random.Range(0.06f, 0.18f);
        }

        if (startTransitionActive && Time.unscaledTime >= startTransitionUntil)
        {
            startTransitionActive = false;
        }
    }

    public void TriggerCrashSting()
    {
        PlayUi(LoopfallCue.CrashSting, 0.55f, 0.04f);
    }

    public void BeginCrashFlicker(float duration)
    {
        crashFlickerActive = true;
        crashFlickerUntil = Time.unscaledTime + Mathf.Max(0.06f, duration);
        nextCrashFlickerTime = Time.unscaledTime;
    }

    public void StopActiveMusicAbruptly()
    {
        crashFlickerActive = false;
        startTransitionActive = false;
        LoopfallMusic.EnsureExists().StopMusicAbruptly();
    }

    public void BeginLoopfallStartTransition(float duration)
    {
        startTransitionActive = true;
        startTransitionUntil = Time.unscaledTime + Mathf.Max(0.2f, duration);
        LoopfallMusic.EnsureExists().SetMenuIntensity(false, false, true);
        PlayUi(LoopfallCue.MenuGlitchTick, 0.08f, 0.02f);
    }

    public void PlayMenuMusic(bool restart = false)
    {
        LoopfallMusic.EnsureExists().PlayMenuMusic(restart);
    }

    public void PlayGameplayMusic(bool restart = false)
    {
        LoopfallMusic.EnsureExists().PlayGameplayMusic(restart);
    }

    public void RouteForActiveScene(bool restart = false)
    {
        LoopfallMusic.EnsureExists().RouteForActiveScene(restart);
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

        uiSource = CreateChildSource("UI");
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
        UpdateCrashFlicker();
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        LoopfallMusic.EnsureExists().RouteForScene(scene.name, true);
    }

    private void RouteForScene(string sceneName, bool restart)
    {
        LoopfallMusic.EnsureExists().RouteForScene(sceneName, restart);
    }

    private AudioSource CreateChildSource(string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        ConfigureBaseSource(source);
        return source;
    }

    private void ConfigureBaseSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.mute = false;
        source.enabled = true;
        source.ignoreListenerPause = true;
    }

    private void UpdateCrashFlicker()
    {
        if (!crashFlickerActive)
        {
            return;
        }

        if (Time.unscaledTime >= crashFlickerUntil)
        {
            crashFlickerActive = false;
            return;
        }

        if (Time.unscaledTime < nextCrashFlickerTime)
        {
            return;
        }

        LoopfallMusic.EnsureExists().SetMenuIntensity(true, true, false);
        PlayUi(UnityEngine.Random.value > 0.62f ? LoopfallCue.MenuGlitchBurst : LoopfallCue.MenuGlitchTick, UnityEngine.Random.Range(0.08f, 0.18f), 0.16f);

        nextCrashFlickerTime = Time.unscaledTime + UnityEngine.Random.Range(0.014f, 0.032f);
    }

    private AudioClip BuildClip(LoopfallCue cue)
    {
        return cue switch
        {
            LoopfallCue.ButtonHover => CreateClip("ButtonHover", 0.09f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.004f, 0.08f);
                float freq = Mathf.Lerp(980f, 1380f, t / 0.09f);
                return (Osc(freq, t, WaveShape.Sine) * 0.6f + Osc(freq * 1.6f, t, WaveShape.Sine) * 0.18f) * env;
            }),
            LoopfallCue.ButtonClick => CreateClip("ButtonClick", 0.12f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.003f, 0.12f);
                float thunk = Osc(180f, t, WaveShape.Triangle) * 0.55f;
                float snap = Noise(i * 2) * 0.12f;
                return (thunk + snap) * env;
            }),
            LoopfallCue.MenuDrone => CreateClip("MenuDrone", 4f, (i, t) =>
            {
                float drift = 0.85f + Mathf.Sin(t * 0.4f) * 0.1f;
                float tone = Osc(43f, t, WaveShape.Sine) * 0.42f + Osc(57f * drift, t, WaveShape.Sine) * 0.22f + Osc(71f, t, WaveShape.Triangle) * 0.08f;
                float grit = Noise(i) * 0.04f;
                return (tone + grit) * 0.6f;
            }),
            LoopfallCue.MenuWarmPad => CreateClip("MenuWarmPad", 4f, (i, t) =>
            {
                float shimmer = 1f + Mathf.Sin(t * 0.55f) * 0.015f;
                float tone = Osc(164f * shimmer, t, WaveShape.Sine) * 0.22f + Osc(206f, t, WaveShape.Sine) * 0.16f + Osc(247f, t, WaveShape.Triangle) * 0.08f;
                return tone * 0.48f;
            }),
            LoopfallCue.MenuStatic => CreateClip("MenuStatic", 3f, (i, t) =>
            {
                float mod = 0.45f + Mathf.Sin(t * 18f) * 0.15f + Mathf.Sin(t * 7f) * 0.08f;
                return Noise(i) * mod * 0.25f;
            }),
            LoopfallCue.MenuGlitchTick => CreateClip("MenuGlitchTick", 0.08f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.002f, 0.08f);
                float freq = Mathf.Lerp(1400f, 620f, t / 0.08f);
                return (Osc(freq, t, WaveShape.Square) * 0.35f + Noise(i) * 0.22f) * env;
            }),
            LoopfallCue.MenuGlitchBurst => CreateClip("MenuGlitchBurst", 0.16f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.001f, 0.16f);
                float freq = Mathf.Lerp(1800f, 220f, t / 0.16f);
                float gate = Mathf.Sign(Mathf.Sin(t * 180f));
                return ((Osc(freq, t, WaveShape.Saw) * 0.3f * gate) + Noise(i) * 0.28f) * env;
            }),
            LoopfallCue.CrashSting => CreateClip("CrashSting", 0.82f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.005f, 0.82f);
                float freq = Mathf.Lerp(190f, 42f, t / 0.82f);
                float body = Osc(freq, t, WaveShape.Saw) * 0.48f + Osc(freq * 0.5f, t, WaveShape.Sine) * 0.24f;
                float grit = Noise(i * 3) * 0.2f;
                return (body + grit) * env;
            }),
            LoopfallCue.WorldWind => CreateClip("WorldWind", 4f, (i, t) =>
            {
                float drift = 0.52f + Mathf.Sin(t * 0.7f) * 0.18f + Mathf.Sin(t * 2.2f) * 0.08f;
                return Noise(i) * drift * 0.22f;
            }),
            LoopfallCue.WorldTone => CreateClip("WorldTone", 4f, (i, t) =>
            {
                return (Osc(52f, t, WaveShape.Sine) * 0.36f + Osc(78f, t, WaveShape.Sine) * 0.12f) * 0.55f;
            }),
            LoopfallCue.FountainLoop => CreateClip("FountainLoop", 3.2f, (i, t) =>
            {
                float water = Noise(i) * (0.2f + Mathf.Sin(t * 9f) * 0.04f);
                float bubble = Mathf.Max(0f, Mathf.Sin(t * 6.5f) - 0.7f) * Osc(420f, t, WaveShape.Sine) * 0.18f;
                return water + bubble;
            }),
            LoopfallCue.ShrineLoop => CreateClip("ShrineLoop", 3.6f, (i, t) =>
            {
                float shimmer = 1f + Mathf.Sin(t * 1.7f) * 0.02f;
                float hum = Osc(110f * shimmer, t, WaveShape.Sine) * 0.28f + Osc(220f, t, WaveShape.Sine) * 0.14f + Osc(330f, t, WaveShape.Sine) * 0.08f;
                return hum * 0.55f;
            }),
            LoopfallCue.FragmentLoop => CreateClip("FragmentLoop", 2.4f, (i, t) =>
            {
                float sparkle = Mathf.Max(0f, Mathf.Sin(t * 8f) - 0.85f) * Osc(920f, t, WaveShape.Sine) * 0.26f;
                float bed = Osc(280f, t, WaveShape.Sine) * 0.08f;
                return bed + sparkle;
            }),
            LoopfallCue.FragmentPickupGood => CreateClip("FragmentPickupGood", 0.58f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.01f, 0.58f);
                float tone = Osc(Mathf.Lerp(520f, 880f, t / 0.58f), t, WaveShape.Sine) * 0.46f;
                float harmonic = Osc(Mathf.Lerp(780f, 1320f, t / 0.58f), t, WaveShape.Sine) * 0.18f;
                return (tone + harmonic) * env;
            }),
            LoopfallCue.FragmentPickupBad => CreateClip("FragmentPickupBad", 0.52f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.005f, 0.52f);
                float tone = Osc(Mathf.Lerp(420f, 160f, t / 0.52f), t, WaveShape.Triangle) * 0.42f;
                float grit = Noise(i) * 0.08f;
                return (tone + grit) * env;
            }),
            LoopfallCue.ShrineAccept => CreateClip("ShrineAccept", 0.85f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.04f, 0.85f);
                float swell = Osc(180f, t, WaveShape.Sine) * 0.34f + Osc(270f, t, WaveShape.Sine) * 0.22f + Osc(360f, t, WaveShape.Sine) * 0.16f;
                return swell * env;
            }),
            LoopfallCue.ShrineReject => CreateClip("ShrineReject", 0.42f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.004f, 0.42f);
                float buzz = Osc(Mathf.Lerp(190f, 110f, t / 0.42f), t, WaveShape.Saw) * 0.34f + Noise(i) * 0.08f;
                return buzz * env;
            }),
            LoopfallCue.DecayWarning => CreateClip("DecayWarning", 0.28f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.003f, 0.28f);
                float creak = Osc(Mathf.Lerp(120f, 76f, t / 0.28f), t, WaveShape.Saw) * 0.26f;
                return (creak + Noise(i) * 0.12f) * env;
            }),
            LoopfallCue.DecayCollapse => CreateClip("DecayCollapse", 1.1f, (i, t) =>
            {
                float env = BurstEnvelope(t, 0.001f, 1.1f);
                float rumble = Osc(Mathf.Lerp(70f, 24f, t / 1.1f), t, WaveShape.Sine) * 0.45f;
                float debris = Noise(i) * 0.28f;
                return (rumble + debris) * env;
            }),
            LoopfallCue.RollLoop => CreateClip("RollLoop", 1.5f, (i, t) =>
            {
                float rumble = Noise(i) * 0.18f + Osc(96f, t, WaveShape.Sine) * 0.07f;
                float grit = Mathf.Abs(Mathf.Sin(t * 20f)) * Noise(i * 2) * 0.08f;
                return rumble + grit;
            }),
            _ => CreateClip("Silence", 0.2f, (i, t) => 0f),
        };
    }

    private AudioClip CreateClip(string name, float duration, Func<int, float, float> generator)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = Mathf.Clamp(generator(i, t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float BurstEnvelope(float t, float attack, float duration)
    {
        float attackPhase = Mathf.Clamp01(t / Mathf.Max(0.0001f, attack));
        float releasePhase = Mathf.Clamp01(1f - t / Mathf.Max(0.0001f, duration));
        return attackPhase * releasePhase;
    }

    private static float Noise(int seed)
    {
        return Mathf.PerlinNoise(seed * 0.017f, seed * 0.031f) * 2f - 1f;
    }

    private static float Osc(float frequency, float time, WaveShape shape)
    {
        float phase = time * frequency;
        float wrapped = phase - Mathf.Floor(phase);

        return shape switch
        {
            WaveShape.Sine => Mathf.Sin(time * frequency * Mathf.PI * 2f),
            WaveShape.Square => wrapped < 0.5f ? 1f : -1f,
            WaveShape.Triangle => 1f - 4f * Mathf.Abs(wrapped - 0.5f),
            WaveShape.Saw => wrapped * 2f - 1f,
            _ => 0f,
        };
    }

    private enum WaveShape
    {
        Sine,
        Square,
        Triangle,
        Saw,
    }
}
