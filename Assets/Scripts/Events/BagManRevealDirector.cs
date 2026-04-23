using UnityEngine;
using System.Collections;

public class BagManRevealDirector : MonoBehaviour
{
    public static bool ChaseActive = false;

    [Header("References")]
    public CatController catController;
    public BagManEnemy bagManEnemy;
    public Camera mainCamera;
    public PSXCameraFollow cameraFollow;
    public Transform bagManTransform;

    [Header("Audio")]
    public AudioSource windAudio;
    public AudioSource scaryMusicSource;
    public AudioClip scaryMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.8f;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;

    [Header("Camera Zoom")]
    public float zoomDuration = 2f;
    public float zoomFOV = 40f;

    [Header("Timing")]
    public float pauseBeforeChase = 2.5f;
    public float windFadeDuration = 2f;

    float originalFOV;
    bool triggered = false;

    void Start()
    {
        originalFOV = mainCamera.fieldOfView;
        ChaseActive = false;
    }

    public void ResetTrigger()
    {
        StopAllCoroutines();
        triggered = false;
        ChaseActive = false;

        if (scaryMusicSource) scaryMusicSource.Stop();
        if (footstepSource) footstepSource.Stop();

        mainCamera.fieldOfView = originalFOV;

        if (cameraFollow)
        {
            cameraFollow.frozen = false;
            cameraFollow.frontMode = false;
            cameraFollow.blendingBack = false;
            cameraFollow.chaseTarget = null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        if (catController) catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        StartCoroutine(FadeAudio(windAudio, 0f, windFadeDuration));

        yield return StartCoroutine(ZoomOntoBagMan());

        yield return new WaitForSeconds(pauseBeforeChase);

        yield return StartCoroutine(SetFOV(mainCamera.fieldOfView, originalFOV, 0.4f));

        if (cameraFollow)
        {
            cameraFollow.chaseTarget = bagManTransform;
            cameraFollow.frozen = false;
            cameraFollow.frontMode = true;
            cameraFollow.blendingBack = false;
        }

        if (catController) catController.UnfreezeMovement();

        if (scaryMusicSource && scaryMusicClip)
        {
            scaryMusicSource.clip = scaryMusicClip;
            scaryMusicSource.volume = musicVolume;
            scaryMusicSource.Play();
        }

        if (bagManEnemy) bagManEnemy.RushToPoint(catController.transform);

        if (footstepSource && footstepClip)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.Play();
        }

        ChaseActive = true;
    }

    IEnumerator ZoomOntoBagMan()
    {
        if (bagManTransform == null)
        {
            yield return StartCoroutine(SetFOV(originalFOV, zoomFOV, zoomDuration));
            yield break;
        }

        float elapsed = 0f;
        Quaternion startRot = mainCamera.transform.rotation;
        Vector3 dirToBagMan = bagManTransform.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dirToBagMan.normalized);

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            mainCamera.fieldOfView = Mathf.Lerp(originalFOV, zoomFOV, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCamera.fieldOfView = zoomFOV;
        mainCamera.transform.rotation = targetRot;
    }

    IEnumerator SetFOV(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCamera.fieldOfView = Mathf.Lerp(from, to, t);
            yield return null;
        }
        mainCamera.fieldOfView = to;
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
        if (targetVolume == 0f) source.Stop();
    }
}