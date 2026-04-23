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
    public Transform zoomTarget;
    public float zoomOutTime = 1.5f;
    public float screamAfter = 1f;
    public float holdTime = 2.5f;
    public float zoomReturnTime = 1.0f;

    [Header("Freeze")]
    public float freezeDuration = 4f;

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
        catController.FreezeMovement();

        var followCam = gameplayCamera.GetComponent<PSXCameraFollow>();
        if (followCam) followCam.frozen = true;

        Vector3 startPos = gameplayCamera.transform.position;
        Quaternion startRot = gameplayCamera.transform.rotation;
        Vector3 targetPos = zoomTarget.position;
        Quaternion targetRot = zoomTarget.rotation;

        bool screamPlayed = false;
        float t = 0f;
        while (t < zoomOutTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / zoomOutTime);
            gameplayCamera.transform.position = Vector3.Lerp(startPos, targetPos, k);
            gameplayCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);

            if (!screamPlayed && t >= screamAfter)
            {
                screamPlayed = true;
                if (audioSource && screamClip)
                    audioSource.PlayOneShot(screamClip, screamVolume);
            }

            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        t = 0f;
        while (t < zoomReturnTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / zoomReturnTime);
            gameplayCamera.transform.position = Vector3.Lerp(targetPos, startPos, k);
            gameplayCamera.transform.rotation = Quaternion.Slerp(targetRot, startRot, k);
            yield return null;
        }

        if (followCam) followCam.frozen = false;
        catController.UnfreezeMovement();
    }
}