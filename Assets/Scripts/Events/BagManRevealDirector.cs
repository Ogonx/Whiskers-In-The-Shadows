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

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        // 1 - Freeze cat
        if (catController) catController.FreezeMovement();

        // 2 - Freeze camera
        if (cameraFollow) cameraFollow.frozen = true;

        // 3 - Fade wind out
        StartCoroutine(FadeAudio(windAudio, 0f, windFadeDuration));

        // 4 - Zoom and rotate camera toward BagMan
        yield return StartCoroutine(ZoomOntoBagMan());

        // 5 - Hold on BagMan
        yield return new WaitForSeconds(pauseBeforeChase);

        // 6 - Unfreeze camera, switch to front mode
        if (cameraFollow)
        {
            cameraFollow.frozen = false;
            cameraFollow.frontMode = true;
        }

        // 7 - Restore FOV
        yield return StartCoroutine(SetFOV(mainCamera.fieldOfView, originalFOV, 0.4f));

        // 8 - Unfreeze cat
        if (catController) catController.UnfreezeMovement();

        // 9 - Play scary music
        if (scaryMusicSource && scaryMusicClip)
        {
            scaryMusicSource.clip = scaryMusicClip;
            scaryMusicSource.volume = musicVolume;
            scaryMusicSource.Play();
        }

        // 10 - BagMan chases cat
        if (bagManEnemy) bagManEnemy.RushToPoint(catController.transform);

        // 11 - Play footsteps
        if (footstepSource && footstepClip)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.Play();
        }

        // 12 - Activate chase flag
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
    }
}