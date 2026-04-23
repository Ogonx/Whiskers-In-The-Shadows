using UnityEngine;
using System.Collections;

public class WindZoneTrigger : MonoBehaviour
{
    public AudioSource windSource;
    public float fadeDuration = 1.5f;
    [Range(0f, 1f)] public float outsideVolume = 1f;

    Coroutine fadeRoutine;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(0f);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(outsideVolume);
    }

    void Fade(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeWind(target));
    }

    IEnumerator FadeWind(float target)
    {
        float start = windSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        windSource.volume = target;
    }
}