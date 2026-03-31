using UnityEngine;
using System.Collections;

public class BedroomDiscoveryTrigger : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform ownerTransform;
    public BagManHousePatrol bagManPatrol;

    [Header("Audio")]
    public AudioSource groanSource;
    public AudioSource catMeowSource;
    public AudioClip catMeowClip;
    public AudioSource atmosphericMusicSource;
    public AudioClip atmosphericMusicClip;
    [Range(0f, 1f)] public float atmosphericMusicVolume = 0.5f;
    public float musicFadeInDuration = 3f;

    [Header("Camera Zoom")]
    public float zoomDuration = 2f;
    public float zoomFOV = 40f;
    public float holdDuration = 3f;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(DiscoverySequence());
    }

    IEnumerator DiscoverySequence()
    {
        if (groanSource) groanSource.Stop();

        if (bagManPatrol)
        {
            bagManPatrol.isWalking = false;
            bagManPatrol.StopAllCoroutines();
        }

        catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        if (catMeowSource && catMeowClip)
        {
            catMeowSource.pitch = 0.8f;
            catMeowSource.PlayOneShot(catMeowClip);
        }

        yield return new WaitForSeconds(1.2f);

        if (catMeowSource && catMeowClip)
        {
            catMeowSource.pitch = 0.75f;
            catMeowSource.PlayOneShot(catMeowClip);
        }

        yield return new WaitForSeconds(0.8f);

        yield return StartCoroutine(ZoomOnOwner());

        yield return new WaitForSeconds(holdDuration);

        yield return StartCoroutine(ResetFOV());

        if (cameraFollow)
        {
            cameraFollow.blendStartPos = mainCamera.transform.position;
            cameraFollow.blendStartRot = mainCamera.transform.rotation;
            cameraFollow.blendBackTimer = 0f;
            cameraFollow.blendBackDuration = 1f;
            cameraFollow.blendingBack = true;
            cameraFollow.frozen = false;
        }

        catController.UnfreezeMovement();

        if (bagManPatrol) bagManPatrol.StartPatrol();

        yield return StartCoroutine(FadeInMusic());

        gameObject.SetActive(false);
    }

    IEnumerator ZoomOnOwner()
    {
        if (ownerTransform == null) yield break;

        float elapsed = 0f;
        float startFOV = mainCamera.fieldOfView;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 dirToOwner = ownerTransform.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dirToOwner.normalized);

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, zoomFOV, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCamera.fieldOfView = zoomFOV;
        mainCamera.transform.rotation = targetRot;
    }

    IEnumerator ResetFOV()
    {
        float startFOV = mainCamera.fieldOfView;
        float elapsed = 0f;
        float resetDuration = 0.5f;
        while (elapsed < resetDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / resetDuration);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, 60f, t);
            yield return null;
        }
        mainCamera.fieldOfView = 60f;
    }

    IEnumerator FadeInMusic()
    {
        if (atmosphericMusicSource == null || atmosphericMusicClip == null) yield break;
        atmosphericMusicSource.clip = atmosphericMusicClip;
        atmosphericMusicSource.loop = true;
        atmosphericMusicSource.volume = 0f;
        atmosphericMusicSource.Play();
        float elapsed = 0f;
        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            atmosphericMusicSource.volume = Mathf.Lerp(0f, atmosphericMusicVolume, elapsed / musicFadeInDuration);
            yield return null;
        }
        atmosphericMusicSource.volume = atmosphericMusicVolume;
    }
}