using System.Collections;
using UnityEngine;

public class BagManJumpscareDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Camera cam;
    [SerializeField] BagManEnemy bagMan;
    [SerializeField] PSXCameraFollow followCam;

    [Header("BagMan Path")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform endPoint;

    [Header("Camera Pan To BagMan")]
    [SerializeField] float panToEnemyDuration = 1.0f;
    [SerializeField] float holdOnEnemyTime = 2.5f;
    [SerializeField] float panBackDuration = 1.0f;

    [Header("Camera Shake")]
    [SerializeField] float shakeDuration = 0.5f;
    [SerializeField] float shakeMagnitude = 0.05f;

    [Header("Audio")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip scareSting;
    [SerializeField] AudioClip footstepsClip;

    [Header("Disable During Scare")]
    [SerializeField] MonoBehaviour[] disablePlayerScripts;

    bool played;

    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        if (!cam) cam = Camera.main;

        // freeze player
        foreach (var mb in disablePlayerScripts)
            if (mb) mb.enabled = false;

        // freeze rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;

        // stop walk animation
        Animator catAnim = player.GetComponent<Animator>();
        if (catAnim) catAnim.SetFloat("Speed", 0f);

        // disable follow camera
        if (followCam) followCam.enabled = false;

        // play footsteps and sting
        if (sfxSource && footstepsClip)
            sfxSource.PlayOneShot(footstepsClip);
        if (sfxSource && scareSting)
            sfxSource.PlayOneShot(scareSting);

        // spawn bagman at point 1
        bagMan.gameObject.SetActive(true);
        bagMan.transform.position = spawnPoint.position;
        bagMan.transform.LookAt(new Vector3(endPoint.position.x, spawnPoint.position.y, endPoint.position.z));
        bagMan.RushToPoint(endPoint);

        // camera shake
        StartCoroutine(ShakeCamera(cam.transform, shakeDuration, shakeMagnitude));

        // pan camera to bagman
        Transform camT = cam.transform;
        Quaternion startRot = camT.rotation;
        Vector3 lookPoint = bagMan.transform.position + Vector3.up * 1.3f;
        Quaternion targetRot = Quaternion.LookRotation(lookPoint - camT.position);

        float t = 0f;
        while (t < panToEnemyDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / panToEnemyDuration);
            camT.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        // hold on bagman while he runs
        float holdTimer = 0f;
        while (holdTimer < holdOnEnemyTime)
        {
            holdTimer += Time.deltaTime;
            // keep camera tracking bagman while he runs
            if (bagMan.gameObject.activeSelf)
            {
                Vector3 trackPoint = bagMan.transform.position + Vector3.up * 1.3f;
                Quaternion trackRot = Quaternion.LookRotation(trackPoint - camT.position);
                camT.rotation = Quaternion.Slerp(camT.rotation, trackRot, Time.deltaTime * 3f);
            }
            yield return null;
        }

        // pan back to player
        Quaternion panBackStart = camT.rotation;
        Quaternion panBackTarget = startRot;

        t = 0f;
        while (t < panBackDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / panBackDuration);
            camT.rotation = Quaternion.Slerp(panBackStart, panBackTarget, k);
            yield return null;
        }

        // re-enable everything
        if (followCam) followCam.enabled = true;

        foreach (var mb in disablePlayerScripts)
            if (mb) mb.enabled = true;
    }

    IEnumerator ShakeCamera(Transform camT, float duration, float magnitude)
    {
        Vector3 originalPos = camT.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            camT.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            yield return null;
        }

        camT.localPosition = originalPos;
    }
}