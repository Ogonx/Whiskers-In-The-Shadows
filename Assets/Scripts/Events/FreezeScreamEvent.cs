using System.Collections;
using UnityEngine;

public class FreezeScreamEvent : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public Camera gameplayCamera;

    [Header("Scream Audio")]
    public AudioSource audioSource;
    public AudioClip screamClip;
    [Range(0f, 1f)] public float screamVolume = 1f;

    [Header("Camera")]
    public Transform zoomTarget;       // the position and rotation the camera zooms to
    public float zoomOutTime = 1.5f;   // how long the zoom to target takes
    public float screamAfter = 1f;     // how far into the zoom the scream plays
    public float holdTime = 2.5f;      // how long to hold on the zoom target
    public float zoomReturnTime = 1.0f; // how long to take returning to original position

    [Header("Freeze")]
    public float freezeDuration = 4f; // total freeze duration

    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;
        triggered = true;
        StartCoroutine(FreezeScreamRoutine());
    }

    IEnumerator FreezeScreamRoutine()
    {
        catController.FreezeMovement(); // stop the cat

        var followCam = gameplayCamera.GetComponent<PSXCameraFollow>();
        if (followCam) followCam.frozen = true; // freeze follow camera

        Vector3 startPos = gameplayCamera.transform.position;
        Quaternion startRot = gameplayCamera.transform.rotation;
        Vector3 targetPos = zoomTarget.position;
        Quaternion targetRot = zoomTarget.rotation;

        bool screamPlayed = false;
        float t = 0f;

        // zoom toward the target position
        while (t < zoomOutTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / zoomOutTime);
            gameplayCamera.transform.position = Vector3.Lerp(startPos, targetPos, k);
            gameplayCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);

            // play the scream mid-zoom at the right moment
            if (!screamPlayed && t >= screamAfter)
            {
                screamPlayed = true;
                if (audioSource && screamClip)
                    audioSource.PlayOneShot(screamClip, screamVolume);
            }

            yield return null;
        }

        yield return new WaitForSeconds(holdTime); // hold on the zoom target

        // return camera to original position
        t = 0f;
        while (t < zoomReturnTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / zoomReturnTime);
            gameplayCamera.transform.position = Vector3.Lerp(targetPos, startPos, k);
            gameplayCamera.transform.rotation = Quaternion.Slerp(targetRot, startRot, k);
            yield return null;
        }

        if (followCam) followCam.frozen = false; // unfreeze follow camera
        catController.UnfreezeMovement();        // give control back to player
    }
}