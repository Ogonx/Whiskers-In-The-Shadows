using UnityEngine;
using System.Collections;

public class WindZoneTrigger : MonoBehaviour
{
    public AudioSource windSource;
    public float fadeDuration = 1.5f; // how long the fade takes
    [Range(0f, 1f)] public float outsideVolume = 1f; // volume to return to when outside

    Coroutine fadeRoutine; // stored so a new fade can cancel the previous one

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(0f); // entering enclosed space, fade wind to silence
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(outsideVolume); // leaving enclosed space, fade wind back in
    }

    void Fade(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine); // cancel any running fade
        fadeRoutine = StartCoroutine(FadeWind(target));
    }

    IEnumerator FadeWind(float target)
    {
        float start = windSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(start, target, elapsed / fadeDuration); // gradually change volume
            yield return null;
        }
        windSource.volume = target;
    }
}