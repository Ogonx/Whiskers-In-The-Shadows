using UnityEngine;
using System.Collections;

public class BagManEscapeTrigger : MonoBehaviour
{
    [Header("References")]
    public BagManEnemy bagManEnemy;
    public CatController catController;
    public Camera mainCamera;
    public PSXCameraFollow cameraFollow;
    public Transform forestExitPoint; // where BagMan runs to when the chase ends

    [Header("Audio")]
    public AudioSource scaryMusicSource;  // chase music to fade out
    public AudioSource footstepSource;    // BagMan footsteps to stop
    public AudioSource windAudio;         // wind to fade back in
    public float musicFadeDuration = 2f;  // how long the music takes to fade out
    public float windFadeInDuration = 2f; // how long the wind takes to fade in

    [Header("Timing")]
    public float watchBagManDuration = 2f; // how long to watch BagMan run away before hiding him

    bool triggered = false; // stops the trigger firing more than once

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!BagManRevealDirector.ChaseActive) return; // only fire if a chase is actually happening
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(EndChase());
    }

    IEnumerator EndChase()
    {
        if (catController) catController.FreezeMovement(); // stop the cat
        if (cameraFollow) cameraFollow.frozen = true;      // lock the camera
        if (footstepSource) footstepSource.Stop();         // stop BagMan footsteps

        if (bagManEnemy && forestExitPoint)
            bagManEnemy.RushToPoint(forestExitPoint); // send BagMan running away

        yield return new WaitForSeconds(watchBagManDuration); // watch him run

        if (bagManEnemy) bagManEnemy.Hide(); // disappear BagMan once he's far enough

        if (cameraFollow)
        {
            // set up a smooth blend back to normal follow camera
            cameraFollow.blendStartPos = mainCamera.transform.position;
            cameraFollow.blendStartRot = mainCamera.transform.rotation;
            cameraFollow.blendBackTimer = 0f;
            cameraFollow.blendBackDuration = 1.5f;
            cameraFollow.blendingBack = true;
            cameraFollow.frontMode = false; // back to normal follow mode
            cameraFollow.frozen = false;    // unfreeze camera
        }

        if (catController) catController.UnfreezeMovement(); // give control back to player

        BagManRevealDirector.ChaseActive = false; // tell the rest of the game the chase is over

        StartCoroutine(FadeWindIn());         // start fading wind back in
        yield return StartCoroutine(FadeMusic()); // fade out chase music

        gameObject.SetActive(false); // disable this trigger so it cant fire again
    }

    IEnumerator FadeMusic()
    {
        if (scaryMusicSource == null) yield break;
        float start = scaryMusicSource.volume;
        float elapsed = 0f;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.deltaTime;
            scaryMusicSource.volume = Mathf.Lerp(start, 0f, elapsed / musicFadeDuration); // fade music out
            yield return null;
        }
        scaryMusicSource.volume = 0f;
        scaryMusicSource.Stop();
    }

    IEnumerator FadeWindIn()
    {
        if (windAudio == null) yield break;
        if (!windAudio.isPlaying) windAudio.Play(); // start wind if not already playing
        float elapsed = 0f;
        while (elapsed < windFadeInDuration)
        {
            elapsed += Time.deltaTime;
            windAudio.volume = Mathf.Lerp(0f, 1f, elapsed / windFadeInDuration); // fade wind in
            yield return null;
        }
        windAudio.volume = 1f;
    }
}