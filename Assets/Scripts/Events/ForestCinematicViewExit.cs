using UnityEngine;
using System.Collections;

public class ForestCinematicViewExit : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public ForestCinematicView forestCinematicView; // reference to stop the high camera tracking

    [Header("Audio")]
    public AudioSource ambientMusicSource;
    public AudioSource windAudio;
    public float musicFadeOutDuration = 2f;
    public float windFadeInDuration = 2f;

    [Header("Camera Blend")]
    public float blendBackDuration = 3f; // how long the camera takes to blend back to normal follow

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        if (forestCinematicView) forestCinematicView.StopTracking(); // stop the high camera coroutine

        if (cameraFollow)
        {
            // set up a smooth blend back to normal follow camera
            cameraFollow.blendStartPos = mainCamera.transform.position;
            cameraFollow.blendStartRot = mainCamera.transform.rotation;
            cameraFollow.blendBackTimer = 0f;
            cameraFollow.blendBackDuration = blendBackDuration;
            cameraFollow.blendingBack = true;
            cameraFollow.frontMode = false;
            cameraFollow.frozen = false;
        }

        StartCoroutine(FadeAudio(ambientMusicSource, 0f, musicFadeOutDuration)); // fade music out
        StartCoroutine(FadeInAudio(windAudio, windFadeInDuration));              // fade wind back in

        yield return new WaitForSeconds(blendBackDuration); // wait for camera blend to finish

        catController.UnfreezeMovement(); // give control back to the player
    }

    IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
    {
        if (source == null) yield break;
        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration); // fade out
            yield return null;
        }
        source.volume = targetVolume;
        source.Stop();
    }

    IEnumerator FadeInAudio(AudioSource source, float duration)
    {
        if (source == null) yield break;
        source.volume = 0f;
        source.Play();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, elapsed / duration); // fade in
            yield return null;
        }
        source.volume = 1f;
    }
}