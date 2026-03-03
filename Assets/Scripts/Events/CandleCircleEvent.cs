using System.Collections;
using UnityEngine;

public class CandleCircleEvent : MonoBehaviour
{
    [Header("Candles")]
    public Light[] candleLights;

    [Header("Whispers")]
    public AudioSource whisperSource;
    public AudioClip whisperClip;
    [Range(0f, 1f)] public float whisperVolume = 0.6f;

    [Header("Flicker Before Extinguish")]
    public float flickerTime = 0.5f;

    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;
        triggered = true;
        StartCoroutine(CandleEvent());
    }

    IEnumerator CandleEvent()
    {
        if (whisperSource && whisperClip)
        {
            whisperSource.clip = whisperClip;
            whisperSource.loop = false;
            whisperSource.volume = 0f;
            whisperSource.Play();
            StartCoroutine(FadeAudio(whisperSource, 0f, whisperVolume, 1.5f));
        }

        foreach (var light in candleLights)
        {
            if (light == null) continue;
            yield return StartCoroutine(FlickerAndExtinguish(light));
            yield return new WaitForSeconds(Random.Range(0.2f, 0.6f));
        }
    }

    IEnumerator FlickerAndExtinguish(Light l)
    {
        float t = 0f;
        float baseIntensity = l.intensity;

        while (t < flickerTime)
        {
            t += Time.deltaTime;
            l.intensity = baseIntensity * Random.Range(0.1f, 1f);
            yield return null;
        }

        l.intensity = 0f;
        l.enabled = false;
    }

    IEnumerator FadeAudio(AudioSource src, float from, float to, float dur)
    {
        float t = 0f;
        src.volume = from;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        src.volume = to;
    }
}