using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LoopfallAudioEmitter : MonoBehaviour
{
    [SerializeField] private LoopfallCue cue = LoopfallCue.WorldWind;
    [SerializeField] private float volume = 0.2f;
    [SerializeField] private float pitch = 1f;
    [SerializeField] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 24f;

    private AudioSource source = null!;

    public void Configure(LoopfallCue newCue, float newVolume, float newPitch, float newSpatialBlend, float newMinDistance = 2f, float newMaxDistance = 24f)
    {
        cue = newCue;
        volume = newVolume;
        pitch = newPitch;
        spatialBlend = newSpatialBlend;
        minDistance = newMinDistance;
        maxDistance = newMaxDistance;
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    private void Start()
    {
        LoopfallAudio audio = LoopfallAudio.EnsureExists();
        audio.ConfigureLoopSource(source, cue, volume, pitch, spatialBlend, minDistance, maxDistance);
        source.Play();
    }
}
