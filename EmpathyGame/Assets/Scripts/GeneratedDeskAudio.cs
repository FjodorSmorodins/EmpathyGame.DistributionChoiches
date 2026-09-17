using System.Collections.Generic;
using UnityEngine;

// Owns its generated clips and a dedicated source, separate from dialogue/voice audio.
public class GeneratedDeskAudio : MonoBehaviour
{
    public enum Sound { PaperPickup, Writing, StamperPickup, Stamp }
    private readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
    private AudioSource source;
    public static GeneratedDeskAudio For(GameObject owner)
    {
        var audio = owner.GetComponent<GeneratedDeskAudio>();
        return audio != null ? audio : owner.AddComponent<GeneratedDeskAudio>();
    }
    public void Play(Sound sound, float volume, bool loop = false)
    {
        if (volume <= 0) { Stop(); return; }
        if (source == null)
        {
            var speaker = new GameObject("Generated Desk Sound");
            speaker.transform.SetParent(transform, false);
            source = speaker.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1;
            source.dopplerLevel = 0;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.6f;
            source.maxDistance = 6;
        }
        if (!clips.TryGetValue(sound, out var clip))
        {
            clip = Generate(sound);
            clips.Add(sound, clip);
        }
        source.volume = Mathf.Clamp01(volume);
        if (loop && source.isPlaying && source.clip == clip) return;
        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }
    public void SetPosition(Vector3 position)
    {
        if (source != null) source.transform.position = position;
    }
    public void Stop() { if (source != null) source.Stop(); }
    private void OnDisable() => Stop();
    private void OnDestroy()
    {
        foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
        if (source != null) Destroy(source.gameObject);
    }
    private static AudioClip Generate(Sound sound)
    {
        const int rate = 24000;
        float duration = sound == Sound.Writing ? 1.4f : sound == Sound.PaperPickup ? 0.32f : 0.18f;
        var samples = new float[Mathf.RoundToInt(duration * rate)];
        var rng = new System.Random(8107 + (int)sound);
        float low = 0, slow = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float noise = (float)(rng.NextDouble() * 2 - 1);
            low += 0.55f * (noise - low);
            slow += 0.045f * (noise - slow);
            float band = low - slow;
            float sample;
            switch (sound)
            {
                case Sound.PaperPickup:
                    float rustle = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(t * 43) * Mathf.Sin(t * 97));
                    sample = band * rustle * Mathf.Sin(Mathf.PI * t / duration);
                    break;
                case Sound.Writing:
                    float strokes = 0.22f + 0.78f * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 17) * Mathf.Sin(t * 31)), 0.6f);
                    sample = band * strokes * 0.6f;
                    break;
                case Sound.StamperPickup:
                    sample = 0.3f * Mathf.Sin(2 * Mathf.PI * 340 * t) * Mathf.Exp(-t * 50) +
                        band * 0.5f * Mathf.Exp(-t * 35);
                    break;
                default:
                    sample = 0.65f * Mathf.Sin(2 * Mathf.PI * 110 * t) * Mathf.Exp(-t * 32) +
                        band * 0.65f * Mathf.Exp(-t * 65);
                    break;
            }
            // Short fades prevent clicks at start/stop and the writing loop seam.
            float fade = Mathf.Clamp01(t / 0.003f) * Mathf.Clamp01((duration - t) / 0.012f);
            samples[i] = Mathf.Clamp(sample * fade, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create("Generated " + sound, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
