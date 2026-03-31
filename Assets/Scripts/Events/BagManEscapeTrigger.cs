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
    public float musicFadeDuration = 2f;

    [Header("Timing")]
    public float watchBagManDuration = 2f;

    void OnTriggerEnter(Collider other)
    {
        if (!BagManRevealDirector.ChaseActive) return;
        if (!other.CompareTag("Player")) return;
        BagManRevealDirector.ChaseActive = false;
        StartCoroutine(EndChase());
    }

    IEnumerator EndChase()
    {
        // 1 - Freeze cat and camera
        if (catController) catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        // 2 - Stop footsteps
        if (footstepSource) footstepSource.Stop();

        // 3 - BagMan runs to forest exit point
        if (bagManEnemy && forestExitPoint)
            bagManEnemy.RushToPoint(forestExitPoint);

        // 4 - Watch BagMan run into forest
        yield return new WaitForSeconds(watchBagManDuration);

        // 5 - BagMan vanishes
        if (bagManEnemy) bagManEnemy.gameObject.SetActive(false);

        // 6 - Smooth camera blend back behind cat
        if (cameraFollow)
        {
            cameraFollow.blendStartPos = mainCamera.transform.position;
            cameraFollow.blendStartRot = mainCamera.transform.rotation;
            cameraFollow.blendBackTimer = 0f;
            cameraFollow.blendingBack = true;
            cameraFollow.frontMode = false;
            cameraFollow.frozen = false;
        }

        // 7 - Unfreeze cat
        if (catController) catController.UnfreezeMovement();

        // 8 - Fade music and wait for it to finish
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
        scaryMusicSource.Stop();
    }
}