using System.Collections;
using UnityEngine;

public class ClockFadeIn : MonoBehaviour
{
    [SerializeField] AudioSource src;
    [SerializeField] float targetVolume = 0.65f;
    [SerializeField] float fadeTime = 1.8f;

    [Header("3D Sound")]
    [SerializeField] float minDistance = 0.8f;
    [SerializeField] float maxDistance = 7f;

    void Reset()
    {
        src = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (!src) src = GetComponent<AudioSource>();
        if (!src) return;

        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.dopplerLevel = 0f;
        src.volume = 0f;

        if (!src.isPlaying) src.Play();
    }

    void OnEnable()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            yield return null;
        }
        src.volume = targetVolume;
    }
}