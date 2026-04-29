using UnityEngine;
using System.Collections;

public class BackDoorExit : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource windSource;          // the wind audio source to fade in when the cat exits
    public float windFadeInDuration = 2f;   // how long the fade takes in seconds
    [Range(0f, 1f)] public float windTargetVolume = 0.8f; // the volume to fade up to

    bool triggered = false; // stops the trigger firing more than once

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;                    // already fired, ignore
        if (!other.CompareTag("Player")) return;  // only react to the cat
        triggered = true;
        StartCoroutine(FadeInWind());
    }

    IEnumerator FadeInWind()
    {
        if (windSource == null) yield break; // nothing assigned, stop

        windSource.volume = 0f;  // start silent
        windSource.Play();       // start playing

        float elapsed = 0f;

        while (elapsed < windFadeInDuration)
        {
            elapsed += Time.deltaTime;                                                        // count up each frame
            windSource.volume = Mathf.Lerp(0f, windTargetVolume, elapsed / windFadeInDuration); // gradually raise volume
            yield return null;                                                                // wait one frame
        }

        windSource.volume = windTargetVolume; // make sure it lands exactly on the target
    }
}