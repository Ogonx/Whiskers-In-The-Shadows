using UnityEngine;
using System.Collections;
using TMPro;

public class BagManWoodsRevealDirector : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform bagManTransform;
    public GameObject bagManObject;          // BagMan's GameObject, hidden until the reveal
    public Transform bagManFaceTarget;       // camera position and rotation for the BagMan close-up
    public Animator bagManAnimator;
    public AudioSource windSource;
    public BagManChase chasePatrol;          // the chase patrol script activated after dialogue

    [Header("Dialogue")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public AudioSource bagManVoiceSource;
    public AudioClip bagManVoiceClip;
    public float timeBetweenLines = 2.5f;   // pause between each dialogue line

    [Header("Audio")]
    public AudioSource chaseMusic;
    public AudioClip chaseMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    public AudioSource atmosphericMusicSource;
    public float atmosphericFadeOutDuration = 1.5f; // how long atmospheric music takes to fade out

    [Header("Camera")]
    public float panDuration = 1.5f; // how long the pan to BagMan takes
    public float holdDuration = 3f;  // how long to hold on BagMan's face

    bool triggered = false;

    string[] dialogueLines = new string[]
    {
        "...",
        "I see you.",
        "You can't run.",
        "Come back...",
    };

    void Start()
    {
        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        if (dialogueText) dialogueText.text = "";
        if (bagManObject) bagManObject.SetActive(false); // BagMan starts hidden
    }

    public void ResetTrigger()
    {
        StopAllCoroutines();
        triggered = false;

        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        if (dialogueText) dialogueText.text = "";
        if (bagManObject) bagManObject.SetActive(false); // hide BagMan again on reset

        if (chaseMusic) chaseMusic.Stop();
        if (chasePatrol) chasePatrol.StopChase();

        if (bagManAnimator)
        {
            bagManAnimator.SetBool("IsRunning", false);
            bagManAnimator.SetBool("IsWalking", false);
        }

        if (cameraFollow)
        {
            cameraFollow.frozen = false;
            cameraFollow.frontMode = false;
            cameraFollow.blendingBack = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        catController.FreezeMovement();  // stop the cat
        cameraFollow.frozen = true;      // lock the camera

        StartCoroutine(FadeOutAtmosphericMusic()); // fade out background music

        yield return new WaitForSeconds(0.5f);

        if (bagManObject) bagManObject.SetActive(true); // show BagMan

        if (bagManAnimator)
        {
            bagManAnimator.SetBool("IsRunning", false);
            bagManAnimator.SetBool("IsWalking", false); // start in idle pose
        }

        yield return new WaitForSeconds(0.3f);

        if (windSource)
        {
            windSource.volume = 0f;
            windSource.Stop(); // kill wind for atmosphere
        }

        yield return StartCoroutine(PanToBagMan()); // pan camera to BagMan's face

        if (dialogueCanvas) dialogueCanvas.SetActive(true);

        yield return StartCoroutine(ShowDialogue()); // show typewriter dialogue

        if (dialogueCanvas) dialogueCanvas.SetActive(false);

        yield return StartCoroutine(PanBackToCat()); // pan back to the cat

        if (chasePatrol) chasePatrol.StartChase(); // BagMan starts patrolling

        if (chaseMusic && chaseMusicClip)
        {
            chaseMusic.clip = chaseMusicClip;
            chaseMusic.volume = musicVolume;
            chaseMusic.Play(); // start chase music
        }

        StartCoroutine(FadeInWind(1f)); // fade wind back in

        cameraFollow.frozen = false;
        catController.UnfreezeMovement(); // give control back

        gameObject.SetActive(false); // disable this trigger so it wont fire again
    }

    IEnumerator FadeOutAtmosphericMusic()
    {
        if (atmosphericMusicSource == null) yield break;
        float start = atmosphericMusicSource.volume;
        float elapsed = 0f;
        while (elapsed < atmosphericFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            atmosphericMusicSource.volume = Mathf.Lerp(start, 0f, elapsed / atmosphericFadeOutDuration); // fade out
            yield return null;
        }
        atmosphericMusicSource.volume = 0f;
        atmosphericMusicSource.Stop();
    }

    IEnumerator PanToBagMan()
    {
        if (bagManFaceTarget == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 targetPos = bagManFaceTarget.position;   // move camera to face target position
        Quaternion targetRot = bagManFaceTarget.rotation; // and match its rotation

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;

        yield return new WaitForSeconds(holdDuration); // hold on BagMan's face
    }

    IEnumerator PanBackToCat()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 catEyeLevel = catController.transform.position + Vector3.up * 0.5f;
        Vector3 dir = catEyeLevel - startPos;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized); // face toward the cat

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t); // pan back
            yield return null;
        }

        mainCamera.transform.rotation = targetRot;
    }

    IEnumerator ShowDialogue()
    {
        foreach (string line in dialogueLines)
        {
            if (bagManVoiceSource && bagManVoiceClip)
                bagManVoiceSource.PlayOneShot(bagManVoiceClip); // play voice clip with each line

            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(timeBetweenLines); // pause between lines
            if (dialogueText) dialogueText.text = ""; // clear text before next line
        }
    }

    IEnumerator TypeLine(string line)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;                    // add one character at a time
            yield return new WaitForSeconds(0.07f);    // delay between each character
        }
    }

    IEnumerator FadeInWind(float duration)
    {
        if (windSource == null) yield break;
        windSource.volume = 0f;
        windSource.Play();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(0f, 1f, elapsed / duration); // gradually raise wind volume
            yield return null;
        }
        windSource.volume = 1f;
    }
}