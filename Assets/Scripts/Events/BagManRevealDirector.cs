using UnityEngine;
using System.Collections;

public class BagManRevealDirector : MonoBehaviour
{
    public static bool ChaseActive = false; // static flag so other scripts know if a chase is happening

    [Header("References")]
    public CatController catController;
    public BagManEnemy bagManEnemy;
    public Camera mainCamera;
    public PSXCameraFollow cameraFollow;
    public Transform bagManTransform; // BagMan's position used for camera zoom target

    [Header("Audio")]
    public AudioSource windAudio;
    public AudioSource scaryMusicSource;
    public AudioClip scaryMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.8f;

    [Header("Footsteps")]
    public AudioSource footstepSource;  // looping BagMan footstep audio during chase
    public AudioClip footstepClip;

    [Header("Camera Zoom")]
    public float zoomDuration = 2f; // how long the zoom onto BagMan takes
    public float zoomFOV = 40f;     // how zoomed in the camera gets

    [Header("Timing")]
    public float pauseBeforeChase = 2.5f; // how long to hold on BagMan before starting the chase
    public float windFadeDuration = 2f;   // how long wind takes to fade out

    float originalFOV;      // camera FOV saved at start so it can be restored
    bool triggered = false; // stops the reveal firing more than once

    void Start()
    {
        originalFOV = mainCamera.fieldOfView; // save starting FOV
        ChaseActive = false;
    }

    public void ResetTrigger()
    {
        StopAllCoroutines(); // cancel any running sequences
        triggered = false;
        ChaseActive = false;

        if (scaryMusicSource) scaryMusicSource.Stop();
        if (footstepSource) footstepSource.Stop();

        mainCamera.fieldOfView = originalFOV; // restore original FOV

        if (cameraFollow)
        {
            cameraFollow.frozen = false;       // unfreeze camera
            cameraFollow.frontMode = false;    // back to normal follow mode
            cameraFollow.blendingBack = false; // cancel any blend
            cameraFollow.chaseTarget = null;   // clear chase target
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
        if (catController) catController.FreezeMovement(); // stop the cat
        if (cameraFollow) cameraFollow.frozen = true;      // lock the camera

        StartCoroutine(FadeAudio(windAudio, 0f, windFadeDuration)); // fade wind out

        yield return StartCoroutine(ZoomOntoBagMan()); // zoom camera to BagMan

        yield return new WaitForSeconds(pauseBeforeChase); // hold on BagMan

        yield return StartCoroutine(SetFOV(mainCamera.fieldOfView, originalFOV, 0.4f)); // zoom back out

        if (cameraFollow)
        {
            cameraFollow.chaseTarget = bagManTransform; // tell camera to track BagMan
            cameraFollow.frozen = false;
            cameraFollow.frontMode = true;     // switch to front mode so player can see BagMan chasing
            cameraFollow.blendingBack = false;
        }

        if (catController) catController.UnfreezeMovement(); // give control back to player

        if (scaryMusicSource && scaryMusicClip)
        {
            scaryMusicSource.clip = scaryMusicClip;
            scaryMusicSource.volume = musicVolume;
            scaryMusicSource.Play(); // start chase music
        }

        if (bagManEnemy) bagManEnemy.RushToPoint(catController.transform); // BagMan starts chasing

        if (footstepSource && footstepClip)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.Play(); // start looping footstep audio
        }

        ChaseActive = true; // tell other scripts the chase is on
    }

    IEnumerator ZoomOntoBagMan()
    {
        if (bagManTransform == null)
        {
            yield return StartCoroutine(SetFOV(originalFOV, zoomFOV, zoomDuration)); // just zoom if no target
            yield break;
        }

        float elapsed = 0f;
        Quaternion startRot = mainCamera.transform.rotation;
        Vector3 dirToBagMan = bagManTransform.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dirToBagMan.normalized); // rotation to face BagMan

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            mainCamera.fieldOfView = Mathf.Lerp(originalFOV, zoomFOV, t);          // zoom in
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t); // pan to BagMan
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
            mainCamera.fieldOfView = Mathf.Lerp(from, to, t); // smoothly change FOV
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
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration); // gradually change volume
            yield return null;
        }
        source.volume = targetVolume;
        if (targetVolume == 0f) source.Stop(); // stop the source if fading to silence
    }
}