using UnityEngine;
using System.Collections;
using TMPro;

public class BedroomDiscoveryTrigger : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform ownerTransform;
    public Transform cameraZoomTarget;
    public GameObject backDoorBlocker;
    public Transform cameraDiscoveryPosition;

    [Header("Dialogue")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public AudioSource mumbleSource;
    public AudioClip mumbleClip;
    public float timeBetweenLines = 2.5f;

    [Header("Audio")]
    public AudioSource groanSource;
    public AudioSource catMeowSource;
    public AudioClip catMeowClip;
    public AudioSource atmosphericMusicSource;
    public AudioClip atmosphericMusicClip;
    [Range(0f, 1f)] public float atmosphericMusicVolume = 0.5f;
    public float musicFadeInDuration = 3f;
    public AudioSource deathSoundSource;
    public AudioClip deathSoundClip;

    [Header("Camera")]
    public float transitionDuration = 1.5f;
    public float zoomDuration = 2f;
    public float zoomFOV = 20f;

    bool triggered = false;

    string[] dialogueLines = new string[]
    {
        "You found me...",
        "its too late though.",
        "I'm so tired...",
        "go home.",
        "Please...",
        "just go home."
    };

    void Start()
    {
        if (backDoorBlocker) backDoorBlocker.SetActive(true);
        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        if (dialogueText) dialogueText.text = "";
    }

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

        catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        if (cameraDiscoveryPosition != null)
            yield return StartCoroutine(TransitionToDiscoveryPosition());

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

        if (mumbleSource && mumbleClip)
        {
            mumbleSource.clip = mumbleClip;
            mumbleSource.loop = true;
            mumbleSource.Play();
        }

        if (dialogueCanvas) dialogueCanvas.SetActive(true);

        yield return StartCoroutine(ShowDialogue());

        if (mumbleSource) mumbleSource.Stop();

        if (deathSoundSource && deathSoundClip)
            deathSoundSource.PlayOneShot(deathSoundClip);

        yield return new WaitForSeconds(deathSoundClip != null ? deathSoundClip.length : 1f);

        if (dialogueCanvas) dialogueCanvas.SetActive(false);

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

        if (backDoorBlocker) backDoorBlocker.SetActive(false);

        yield return StartCoroutine(FadeInMusic());

        gameObject.SetActive(false);
    }

    IEnumerator TransitionToDiscoveryPosition()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, cameraDiscoveryPosition.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, cameraDiscoveryPosition.rotation, t);
            yield return null;
        }

        mainCamera.transform.position = cameraDiscoveryPosition.position;
        mainCamera.transform.rotation = cameraDiscoveryPosition.rotation;
    }

    IEnumerator ZoomOnOwner()
    {
        float elapsed = 0f;
        float startFOV = mainCamera.fieldOfView;
        Quaternion startRot = mainCamera.transform.rotation;

        Transform zoomTarget = cameraZoomTarget != null ? cameraZoomTarget : ownerTransform;
        if (zoomTarget == null) yield break;

        Vector3 dirToTarget = zoomTarget.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);

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

    IEnumerator ShowDialogue()
    {
        foreach (string line in dialogueLines)
        {
            if (dialogueText) dialogueText.text = "";
            foreach (char letter in line)
            {
                if (dialogueText) dialogueText.text += letter;
                yield return new WaitForSeconds(0.08f);
            }
            yield return new WaitForSeconds(timeBetweenLines);
        }
        if (dialogueText) dialogueText.text = "";
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