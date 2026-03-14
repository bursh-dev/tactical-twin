using UnityEngine;

/// <summary>
/// Generates simple procedural sound effects at runtime.
/// No external audio files needed.
/// </summary>
public static class ProceduralSFX
{
    public static AudioClip GenerateGunshot(float duration = 0.15f, int sampleRate = 44100)
    {
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 40f);
            float noise = (Random.value * 2f - 1f);
            float crack = Mathf.Sin(t * 800f) * Mathf.Exp(-t * 80f);
            data[i] = (noise * 0.6f + crack * 0.4f) * envelope;
        }

        var clip = AudioClip.Create("gunshot", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip GenerateHit(float duration = 0.2f, int sampleRate = 44100)
    {
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 20f);
            float ping = Mathf.Sin(t * 2500f) * Mathf.Exp(-t * 30f);
            float thud = Mathf.Sin(t * 200f) * Mathf.Exp(-t * 15f);
            data[i] = (ping * 0.5f + thud * 0.5f) * envelope;
        }

        var clip = AudioClip.Create("hit", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip GenerateMiss(float duration = 0.08f, int sampleRate = 44100)
    {
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 50f);
            float whoosh = (Random.value * 2f - 1f) * 0.3f;
            data[i] = whoosh * envelope;
        }

        var clip = AudioClip.Create("miss", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
