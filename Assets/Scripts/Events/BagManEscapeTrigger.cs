using UnityEngine;
using System.Collections;

public class BagManEscapeTrigger : MonoBehaviour
{
    [Header("References")]
    public BagManEnemy bagManEnemy;
    public CatController catController;
    public Camera mainCamera;
    public PSXCameraFollow cameraFollow;
    public Transform forestExitPoint;

    [Header("Audio")]
    public AudioSource scaryMusicSource;
    public AudioSource footstepSource;
    public AudioSource windAudio;
    public float musicFadeDuration = 2f;
    public float windFadeInDuration = 2f;

    [Header("Timing")]
    public float watchBagManDuration = 2f;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!BagManRevealDirector.ChaseActive) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(EndChase());
    }

    IEnumerator EndChase()
    {
        if (catController) catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        if (footstepSource) footstepSource.Stop();

        if (bagManEnemy && forestExitPoint)
            bagManEnemy.RushToPoint(forestExitPoint);

        yield return new WaitForSeconds(watchBagManDuration);

        if (bagManEnemy) bagManEnemy.Hide();

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

        if (catController) catController.UnfreezeMovement();

        BagManRevealDirector.ChaseActive = false;

        StartCoroutine(FadeWindIn());

        yield return StartCoroutine(FadeMusic());

        gameObject.SetActive(false);
    }

    IEnumerator FadeMusic()
    {
        if (scaryMusicSource == null) yield break;
        float start = scaryMusicSource.volume;
        float elapsed = 0f;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.deltaTime;
            scaryMusicSource.volume = Mathf.Lerp(start, 0f, elapsed / musicFadeDuration);
            yield return null;
        }
        scaryMusicSource.volume = 0f;
        scaryMusicSource.Stop();
    }

    IEnumerator FadeWindIn()
    {
        if (windAudio == null) yield break;
        if (!windAudio.isPlaying) windAudio.Play();
        float elapsed = 0f;
        while (elapsed < windFadeInDuration)
        {
            elapsed += Time.deltaTime;
            windAudio.volume = Mathf.Lerp(0f, 1f, elapsed / windFadeInDuration);
            yield return null;
        }
        windAudio.volume = 1f;
    }
}