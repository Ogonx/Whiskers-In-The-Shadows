using UnityEngine;
using System.Collections;

public class ForestCinematicView : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;

    [Header("Cat Path")]
    public Transform[] waypoints;  // the path the cat walks along automatically during this sequence
    public float walkSpeed = 2f;

    [Header("Audio")]
    public AudioSource windAudio;
    public AudioSource ambientMusicSource;
    public AudioClip ambientMusicClip;
    public float musicFadeInDuration = 2f;
    public float windFadeDuration = 2f;
    [Range(0f, 1f)] public float musicTargetVolume = 0.8f;

    [Header("Camera Rise")]
    public float riseHeight = 15f;   // how high the camera rises above the cat
    public float risePullBack = 8f;  // how far back the camera pulls
    public float riseDuration = 4f;  // how long the camera takes to reach its high position

    bool triggered = false;
    Coroutine zoomRoutine; // stored so StopTracking can cancel it

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(ReturnSequence());
    }

    public void StopTracking()
    {
        // called by ForestCinematicViewExit to cancel the camera zoom coroutine
        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }
    }

    IEnumerator ReturnSequence()
    {
        catController.FreezeMovement(); // stop the player controlling the cat

        StartCoroutine(FadeAudio(windAudio, 0f, windFadeDuration)); // fade wind out
        StartCoroutine(FadeInMusic());                               // fade ambient music in

        zoomRoutine = StartCoroutine(ZoomOut()); // start the rising camera

        yield return StartCoroutine(WalkCatToHouse()); // walk cat along waypoints automatically
    }

    IEnumerator WalkCatToHouse()
    {
        Rigidbody rb = catController.GetComponent<Rigidbody>();
        Animator animator = catController.GetComponent<Animator>();

        foreach (Transform waypoint in waypoints)
        {
            // move toward each waypoint in turn
            while (Vector3.Distance(catController.transform.position, waypoint.position) > 0.5f)
            {
                Vector3 dir = (waypoint.position - catController.transform.position);
                dir.y = 0f;
                dir.Normalize();

                if (rb)
                {
                    Vector3 vel = rb.linearVelocity;
                    vel.x = dir.x * walkSpeed;
                    vel.z = dir.z * walkSpeed;
                    rb.linearVelocity = vel; // move the cat via rigidbody velocity
                }

                if (dir.sqrMagnitude > 0.01f)
                    catController.transform.rotation = Quaternion.Slerp(
                        catController.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 8f); // smoothly rotate to face direction of travel

                if (animator) animator.SetFloat("Speed", walkSpeed); // play walk animation

                yield return null;
            }
        }

        if (rb) rb.linearVelocity = Vector3.zero;  // stop the cat
        if (animator) animator.SetFloat("Speed", 0f); // stop walk animation
    }

    IEnumerator ZoomOut()
    {
        if (cameraFollow) cameraFollow.frozen = true; // take manual control of the camera

        float elapsed = 0f;

        // phase 1: rise the camera up to its high position
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration);

            Vector3 targetPos = catController.transform.position
                - catController.transform.forward * risePullBack
                + Vector3.up * riseHeight; // high position behind and above the cat

            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, t);

            Vector3 lookTarget = catController.transform.position
                + catController.transform.forward * 10f
                + Vector3.up * 1f;
            Vector3 lookDir = lookTarget - mainCamera.transform.position;
            if (lookDir != Vector3.zero)
                mainCamera.transform.rotation = Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 2f); // smoothly look ahead of the cat

            yield return null;
        }

        // phase 2: keep following the cat from the high position until StopTracking is called
        while (true)
        {
            Vector3 followPos = catController.transform.position
                - catController.transform.forward * risePullBack
                + Vector3.up * riseHeight;

            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position, followPos, Time.deltaTime * 3f); // smoothly track the cat

            Vector3 lookTarget = catController.transform.position
                + catController.transform.forward * 10f
                + Vector3.up * 1f;
            Vector3 lookDir = lookTarget - mainCamera.transform.position;
            if (lookDir != Vector3.zero)
                mainCamera.transform.rotation = Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 2f);

            yield return null;
        }
    }

    IEnumerator FadeInMusic()
    {
        if (ambientMusicSource == null || ambientMusicClip == null) yield break;
        ambientMusicSource.clip = ambientMusicClip;
        ambientMusicSource.volume = 0f;
        ambientMusicSource.Play();
        float elapsed = 0f;
        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            ambientMusicSource.volume = Mathf.Lerp(0f, musicTargetVolume, elapsed / musicFadeInDuration); // fade in
            yield return null;
        }
        ambientMusicSource.volume = musicTargetVolume;
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
        source.Stop();
    }
}