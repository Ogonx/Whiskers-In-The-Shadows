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
    public GameObject bagManObject;
    public Transform bagManFaceTarget;
    public Animator bagManAnimator;
    public AudioSource windSource;
    public BagManChase chasePatrol;

    [Header("Dialogue")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public AudioSource bagManVoiceSource;
    public AudioClip bagManVoiceClip;
    public float timeBetweenLines = 2.5f;

    [Header("Audio")]
    public AudioSource chaseMusic;
    public AudioClip chaseMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    public AudioSource atmosphericMusicSource;
    public float atmosphericFadeOutDuration = 1.5f;

    [Header("Camera")]
    public float panDuration = 1.5f;
    public float holdDuration = 3f;

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
        if (bagManObject) bagManObject.SetActive(false);
    }

    public void ResetTrigger()
    {
        StopAllCoroutines();
        triggered = false;

        if (dialogueCanvas) dialogueCanvas.SetActive(false);
        if (dialogueText) dialogueText.text = "";
        if (bagManObject) bagManObject.SetActive(false);

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
        catController.FreezeMovement();
        cameraFollow.frozen = true;

        StartCoroutine(FadeOutAtmosphericMusic());

        yield return new WaitForSeconds(0.5f);

        if (bagManObject) bagManObject.SetActive(true);

        if (bagManAnimator)
        {
            bagManAnimator.SetBool("IsRunning", false);
            bagManAnimator.SetBool("IsWalking", false);
        }

        yield return new WaitForSeconds(0.3f);

        if (windSource)
        {
            windSource.volume = 0f;
            windSource.Stop();
        }

        yield return StartCoroutine(PanToBagMan());

        if (dialogueCanvas) dialogueCanvas.SetActive(true);

        yield return StartCoroutine(ShowDialogue());

        if (dialogueCanvas) dialogueCanvas.SetActive(false);

        yield return StartCoroutine(PanBackToCat());

        if (chasePatrol) chasePatrol.StartChase();

        if (chaseMusic && chaseMusicClip)
        {
            chaseMusic.clip = chaseMusicClip;
            chaseMusic.volume = musicVolume;
            chaseMusic.Play();
        }

        StartCoroutine(FadeInWind(1f));

        cameraFollow.frozen = false;
        catController.UnfreezeMovement();

        gameObject.SetActive(false);
    }

    IEnumerator FadeOutAtmosphericMusic()
    {
        if (atmosphericMusicSource == null) yield break;
        float start = atmosphericMusicSource.volume;
        float elapsed = 0f;
        while (elapsed < atmosphericFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            atmosphericMusicSource.volume = Mathf.Lerp(start, 0f, elapsed / atmosphericFadeOutDuration);
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

        Vector3 targetPos = bagManFaceTarget.position;
        Quaternion targetRot = bagManFaceTarget.rotation;

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

        yield return new WaitForSeconds(holdDuration);
    }

    IEnumerator PanBackToCat()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 catEyeLevel = catController.transform.position + Vector3.up * 0.5f;
        Vector3 dir = catEyeLevel - startPos;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCamera.transform.rotation = targetRot;
    }

    IEnumerator ShowDialogue()
    {
        foreach (string line in dialogueLines)
        {
            if (bagManVoiceSource && bagManVoiceClip)
                bagManVoiceSource.PlayOneShot(bagManVoiceClip);

            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(timeBetweenLines);
            if (dialogueText) dialogueText.text = "";
        }
    }

    IEnumerator TypeLine(string line)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
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
            windSource.volume = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        windSource.volume = 1f;
    }
}