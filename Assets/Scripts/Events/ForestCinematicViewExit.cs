using UnityEngine;
using System.Collections;

public class ForestCinematicViewExit : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;

    [Header("Audio")]
    public AudioSource ambientMusicSource;
    public AudioSource windAudio;
    public float musicFadeOutDuration = 2f;
    public float windFadeInDuration = 2f;

    [Header("Camera Zoom In")]
    public float zoomInFOV = 60f;
    public float zoomInDuration = 3f;

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
        // 1 - Smooth blend camera back behind cat
        if (cameraFollow)
        {
            cameraFollow.blendStartPos = mainCamera.transform.position;
            cameraFollow.blendStartRot = mainCamera.transform.rotation;
            cameraFollow.blendBackTimer = 0f;
            cameraFollow.blendBackDuration = 1.5f;
            cameraFollow.blendingBack = true;
            cameraFollow.frontMode = false;
            cameraFollow.frozen = false;
        }

        // 2 - Slowly zoom FOV back in
        StartCoroutine(ZoomIn());

        // 3 - Fade music out and wind back in simultaneously
        StartCoroutine(FadeAudio(ambientMusicSource, 0f, musicFadeOutDuration));
        StartCoroutine(FadeInAudio(windAudio, windFadeInDuration));

        // 4 - Wait for zoom to finish
        yield return new WaitForSeconds(zoomInDuration);

        // 5 - Give player control back
        catController.UnfreezeMovement();
    }

    IEnumerator ZoomIn()
    {
        float startFOV = mainCamera.fieldOfView;
        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomInDuration);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, zoomInFOV, t);
            yield return null;
        }
        mainCamera.fieldOfView = zoomInFOV;
    }

    IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
    {
        if (source == null) yield break;
        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
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
            source.volume = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        source.volume = 1f;
    }
}