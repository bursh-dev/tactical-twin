using UnityEngine;

/// <summary>
/// Simple audio manager for playing sound effects.
/// Provides a central point for audio in the POC.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip shotClip;
    public AudioClip hitClip;
    public AudioClip missClip;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayShot() => Play(shotClip);
    public void PlayHit() => Play(hitClip);
    public void PlayMiss() => Play(missClip, 0.5f);

    void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
