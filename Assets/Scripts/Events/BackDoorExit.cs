using UnityEngine;
using System.Collections;

public class BackDoorExit : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource windSource;
    public float windFadeInDuration = 2f;
    [Range(0f, 1f)] public float windTargetVolume = 0.8f;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(FadeInWind());
    }

    IEnumerator FadeInWind()
    {
        if (windSource == null) yield break;
        windSource.volume = 0f;
        windSource.Play();
        float elapsed = 0f;
        while (elapsed < windFadeInDuration)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(0f, windTargetVolume, elapsed / windFadeInDuration);
            yield return null;
        }
        windSource.volume = windTargetVolume;
    }
}