using System.Collections;
using UnityEngine;

public class ClockFadeIn : MonoBehaviour
{
    [SerializeField] AudioSource src;
    [SerializeField] float targetVolume = 0.65f; // volume to fade up to
    [SerializeField] float fadeTime = 1.8f;      // how long the fade takes

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

        src.spatialBlend = 1f; // fully 3D
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.dopplerLevel = 0f;
        src.volume = 0f;  // start silent
        if (!src.isPlaying) src.Play();
    }

    void OnEnable()
    {
        StartCoroutine(FadeIn()); // fade in whenever this object is enabled
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime); // gradually raise volume
            yield return null;
        }
        src.volume = targetVolume;
    }
}