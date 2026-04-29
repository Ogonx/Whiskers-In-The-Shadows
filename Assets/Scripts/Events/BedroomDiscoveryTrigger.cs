using UnityEngine;
using System.Collections;
using TMPro;

public class BedroomDiscoveryTrigger : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform ownerTransform;            // the owner lying on the bed
    public Transform cameraZoomTarget;          // target for the zoom, usually the owner's face
    public GameObject backDoorBlocker;          // invisible wall that stops the player leaving until this sequence plays
    public Transform cameraDiscoveryPosition;   // camera position for the discovery shot

    [Header("Dialogue")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public AudioSource mumbleSource;
    public AudioClip mumbleClip;
    public float timeBetweenLines = 2.5f; // pause between each dialogue line

    [Header("Audio")]
    public AudioSource groanSource;              // ambient groan stopped when sequence fires
    public AudioSource catMeowSource;
    public AudioClip catMeowClip;
    public AudioSource atmosphericMusicSource;
    public AudioClip atmosphericMusicClip;
    [Range(0f, 1f)] public float atmosphericMusicVolume = 0.5f;
    public float musicFadeInDuration = 3f;       // how long music takes to fade in after dialogue
    public AudioSource deathSoundSource;
    public AudioClip deathSoundClip;

    [Header("Camera")]
    public float transitionDuration = 1.5f; // how long the camera takes to move to discovery position
    public float zoomDuration = 2f;         // how long the zoom onto the owner takes
    public float zoomFOV = 20f;             // how zoomed in the camera gets

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
        if (backDoorBlocker) backDoorBlocker.SetActive(true);  // block the exit until sequence plays
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
        if (groanSource) groanSource.Stop(); // stop ambient groaning when sequence starts

        catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        if (cameraDiscoveryPosition != null)
            yield return StartCoroutine(TransitionToDiscoveryPosition()); // move camera to discovery angle

        // play two distressed cat meows with pitch variation
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

        yield return StartCoroutine(ZoomOnOwner()); // zoom in on the owner

        if (mumbleSource && mumbleClip)
        {
            mumbleSource.clip = mumbleClip;
            mumbleSource.loop = true;
            mumbleSource.Play(); // start looping mumble audio under dialogue
        }

        if (dialogueCanvas) dialogueCanvas.SetActive(true);

        yield return StartCoroutine(ShowDialogue()); // show owner dialogue

        if (mumbleSource) mumbleSource.Stop();

        if (deathSoundSource && deathSoundClip)
            deathSoundSource.PlayOneShot(deathSoundClip); // play death sound at end of dialogue

        yield return new WaitForSeconds(deathSoundClip != null ? deathSoundClip.length : 1f);

        if (dialogueCanvas) dialogueCanvas.SetActive(false);

        yield return StartCoroutine(ResetFOV()); // zoom back out

        if (cameraFollow)
        {
            // set up smooth blend back to normal follow camera
            cameraFollow.blendStartPos = mainCamera.transform.position;
            cameraFollow.blendStartRot = mainCamera.transform.rotation;
            cameraFollow.blendBackTimer = 0f;
            cameraFollow.blendBackDuration = 1f;
            cameraFollow.blendingBack = true;
            cameraFollow.frozen = false;
        }

        catController.UnfreezeMovement(); // give control back

        if (backDoorBlocker) backDoorBlocker.SetActive(false); // open the exit now

        yield return StartCoroutine(FadeInMusic()); // fade in atmospheric music

        gameObject.SetActive(false); // disable trigger so it wont fire again
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
            mainCamera.transform.position = Vector3.Lerp(startPos, cameraDiscoveryPosition.position, t); // move camera
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, cameraDiscoveryPosition.rotation, t); // rotate camera
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
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized); // face the owner

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, zoomFOV, t);          // zoom in
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t); // pan to owner
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
                if (dialogueText) dialogueText.text += letter; // add one character at a time
                yield return new WaitForSeconds(0.08f);        // delay between each character
            }
            yield return new WaitForSeconds(timeBetweenLines); // pause before next line
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
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, 60f, t); // zoom back to default FOV
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
            atmosphericMusicSource.volume = Mathf.Lerp(0f, atmosphericMusicVolume, elapsed / musicFadeInDuration); // fade in
            yield return null;
        }
        atmosphericMusicSource.volume = atmosphericMusicVolume;
    }
}