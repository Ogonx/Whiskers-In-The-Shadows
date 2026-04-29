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
    [SerializeField] Transform spawnPoint; // where BagMan appears
    [SerializeField] Transform endPoint;   // where BagMan runs to

    [Header("Camera Pan To BagMan")]
    [SerializeField] float panToEnemyDuration = 1.0f; // how long the camera takes to pan to BagMan
    [SerializeField] float holdOnEnemyTime = 2.5f;    // how long to track BagMan while he runs
    [SerializeField] float panBackDuration = 1.0f;    // how long to pan back to player

    [Header("Camera Shake")]
    [SerializeField] float shakeDuration = 0.5f;   // how long the shake lasts
    [SerializeField] float shakeMagnitude = 0.05f; // how intense the shake is

    [Header("Audio")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip scareSting;    // jumpscare sound
    [SerializeField] AudioClip footstepsClip; // BagMan footstep sound

    [Header("Disable During Scare")]
    [SerializeField] MonoBehaviour[] disablePlayerScripts; // scripts to disable so player cant move

    bool played; // stops the sequence firing more than once

    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        if (!cam) cam = Camera.main;

        // freeze player by disabling their scripts
        foreach (var mb in disablePlayerScripts)
            if (mb) mb.enabled = false;

        // stop the rigidbody moving
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = Vector3.zero;

        // stop walk animation
        Animator catAnim = player.GetComponent<Animator>();
        if (catAnim) catAnim.SetFloat("Speed", 0f);

        // disable the follow camera so we can manually control it
        if (followCam) followCam.enabled = false;

        // play footsteps and scare sting at the same time
        if (sfxSource && footstepsClip) sfxSource.PlayOneShot(footstepsClip);
        if (sfxSource && scareSting) sfxSource.PlayOneShot(scareSting);

        // activate BagMan and send him running toward the end point
        bagMan.gameObject.SetActive(true);
        bagMan.transform.position = spawnPoint.position;
        bagMan.transform.LookAt(new Vector3(endPoint.position.x, spawnPoint.position.y, endPoint.position.z));
        bagMan.RushToPoint(endPoint);

        // shake the camera at the same time BagMan appears
        StartCoroutine(ShakeCamera(cam.transform, shakeDuration, shakeMagnitude));

        // pan camera from player to BagMan
        Transform camT = cam.transform;
        Quaternion startRot = camT.rotation;
        Vector3 lookPoint = bagMan.transform.position + Vector3.up * 1.3f;
        Quaternion targetRot = Quaternion.LookRotation(lookPoint - camT.position);

        float t = 0f;
        while (t < panToEnemyDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / panToEnemyDuration);
            camT.rotation = Quaternion.Slerp(startRot, targetRot, k); // smoothly pan to BagMan
            yield return null;
        }

        // track BagMan while he runs for holdOnEnemyTime seconds
        float holdTimer = 0f;
        while (holdTimer < holdOnEnemyTime)
        {
            holdTimer += Time.deltaTime;
            if (bagMan.gameObject.activeSelf)
            {
                Vector3 trackPoint = bagMan.transform.position + Vector3.up * 1.3f;
                Quaternion trackRot = Quaternion.LookRotation(trackPoint - camT.position);
                camT.rotation = Quaternion.Slerp(camT.rotation, trackRot, Time.deltaTime * 3f); // keep camera on BagMan
            }
            yield return null;
        }

        // pan back to the player
        Quaternion panBackStart = camT.rotation;
        Quaternion panBackTarget = startRot;

        t = 0f;
        while (t < panBackDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / panBackDuration);
            camT.rotation = Quaternion.Slerp(panBackStart, panBackTarget, k); // pan back
            yield return null;
        }

        // re-enable the follow camera and player scripts
        if (followCam) followCam.enabled = true;
        foreach (var mb in disablePlayerScripts)
            if (mb) mb.enabled = true;
    }

    IEnumerator ShakeCamera(Transform camT, float duration, float magnitude)
    {
        Vector3 originalPos = camT.localPosition; // save original position
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude; // random horizontal offset
            float y = Random.Range(-1f, 1f) * magnitude; // random vertical offset
            camT.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            yield return null;
        }

        camT.localPosition = originalPos; // snap back to original position when done
    }
}