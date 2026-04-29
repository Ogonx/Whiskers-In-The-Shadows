using System.Collections;
using UnityEngine;

public class WakeStartDirector : MonoBehaviour
{
    [Header("Run Control")]
    [SerializeField] bool requireWakeFlag = true; // if true, only plays when WakeState flag is set

    [Header("Cutscene UI Root")]
    [SerializeField] GameObject cutsceneUIRoot; // the eyelid canvas, hidden after the sequence ends

    [Header("Eyelids")]
    [SerializeField] RectTransform topLid;
    [SerializeField] RectTransform bottomLid;
    [SerializeField] float openGapY = 18f;      // gap between lids when fully open
    [SerializeField] float lidOverscan = 40f;    // extra travel to ensure lids fully leave screen
    [SerializeField] AnimationCurve lidCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Blur Overlay")]
    [SerializeField] CanvasGroup blurOverlay;
    [Range(0f, 1f)][SerializeField] float blurStartAlpha = 0.55f;
    [SerializeField] float blurFadeTime = 1.1f;

    [Header("Cameras")]
    [SerializeField] Camera wakeCamera;      // the cutscene camera active during the opening
    [SerializeField] Camera gameplayCamera;  // switched to after the sequence
    [SerializeField] float cameraBlendTime = 1.2f; // how long the camera blend takes

    [Header("Hold After Eyes Open")]
    [SerializeField] float holdAfterEyesOpenSeconds = 3f; // how long to hold before blending to gameplay camera

    [Header("Door Shake")]
    [SerializeField] bool enableDoorShake = true;
    [SerializeField] float shakeDuration = 0.55f;
    [SerializeField] float shakePitch = 1.2f;
    [SerializeField] float shakeRoll = 0.8f;
    [SerializeField] float shakeSpeed = 18f;

    [Header("Audio")]
    [SerializeField] AudioSource windSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip doorBangClip;
    [SerializeField] AudioClip windClip;

    [Header("Meow")]
    [SerializeField] AudioClip scaredMeowClip;
    [Range(0f, 1f)][SerializeField] float meowVolume = 0.8f;

    [Header("Timing")]
    [SerializeField] float holdBlackSeconds = 4f;       // how long to hold on black before anything plays
    [SerializeField] float bangDelayAfterBlack = 0.6f;  // delay before door bang
    [SerializeField] float openEyesTime = 1.2f;         // how long the eyelid open animation takes

    [Header("On Wake Enable")]
    [SerializeField] MonoBehaviour[] scriptsToEnable;   // scripts disabled during the sequence, enabled after
    [SerializeField] GameObject[] objectsToEnable;      // objects disabled during the sequence, enabled after

    [Header("Tutorial")]
    [SerializeField] TutorialPromptUI tutorialPrompt; // shown at the end of the sequence

    float lidMove; // calculated lid travel distance based on screen size

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; // hide cursor immediately

        if (requireWakeFlag && !WakeState.PlayWakeSequenceOnLoad) return; // skip if flag not set
        WakeState.PlayWakeSequenceOnLoad = false;

        SetEnabled(scriptsToEnable, false); // disable player scripts during cutscene
        SetActive(objectsToEnable, false);

        SetupCameras();
        SetupUI();

        StartCoroutine(WakeRoutine());
    }

    void SetupCameras()
    {
        wakeCamera.enabled = true;
        gameplayCamera.enabled = false; // start with wake camera active

        if (!wakeCamera.GetComponent<AudioListener>())
            wakeCamera.gameObject.AddComponent<AudioListener>();

        if (!gameplayCamera.GetComponent<AudioListener>())
            gameplayCamera.gameObject.AddComponent<AudioListener>();
    }

    void SetupUI()
    {
        float lidHeight = Mathf.Max(topLid.rect.height, bottomLid.rect.height);
        if (lidHeight < 10f) lidHeight = 600f; // fallback if rect not yet calculated

        lidMove = lidHeight + lidOverscan + (openGapY * 0.5f); // total distance lids travel

        SetLidsClosedInstant(); // start with lids covering screen

        if (blurOverlay) blurOverlay.alpha = 0f;
    }

    IEnumerator WakeRoutine()
    {
        yield return new WaitForSecondsRealtime(holdBlackSeconds);
        yield return new WaitForSecondsRealtime(bangDelayAfterBlack);

        // play door bang and scared meow together
        if (sfxSource)
        {
            if (doorBangClip) sfxSource.PlayOneShot(doorBangClip);
            if (scaredMeowClip) sfxSource.PlayOneShot(scaredMeowClip, meowVolume);
        }

        if (windSource && windClip)
        {
            windSource.clip = windClip;
            windSource.volume = 0.5f;
            windSource.Play();
        }

        // open eyes and shake camera at the same time
        Coroutine eyes = StartCoroutine(OpenEyes(openEyesTime));
        Coroutine shake = enableDoorShake ? StartCoroutine(DoorImpactShake(wakeCamera.transform, shakeDuration)) : null;

        yield return eyes;
        if (shake != null) yield return shake;

        yield return new WaitForSecondsRealtime(holdAfterEyesOpenSeconds);
        yield return BlendToGameplayCamera(); // smooth blend from wake camera to gameplay camera

        if (cutsceneUIRoot) cutsceneUIRoot.SetActive(false); // hide eyelid canvas

        SetActive(objectsToEnable, true);
        SetEnabled(scriptsToEnable, true); // give player control back

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (tutorialPrompt) tutorialPrompt.Show(); // show WASD and Shift hints
    }

    IEnumerator DoorImpactShake(Transform cam, float dur)
    {
        Vector3 baseRot = cam.localEulerAngles;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float fade = 1f - t / dur;                         // shake fades out over time
            float wob = Mathf.Sin(t * shakeSpeed) * fade;      // oscillating wobble
            cam.localEulerAngles = new Vector3(baseRot.x + wob * shakePitch, baseRot.y, baseRot.z + wob * shakeRoll);
            yield return null;
        }

        cam.localEulerAngles = baseRot; // snap back to original rotation
    }

    IEnumerator BlendToGameplayCamera()
    {
        float t = 0f;
        Vector3 p0 = wakeCamera.transform.position;
        Quaternion r0 = wakeCamera.transform.rotation;
        Vector3 p1 = gameplayCamera.transform.position;
        Quaternion r1 = gameplayCamera.transform.rotation;

        while (t < cameraBlendTime)
        {
            t += Time.deltaTime;
            float k = t / cameraBlendTime;
            wakeCamera.transform.position = Vector3.Lerp(p0, p1, k);       // move toward gameplay camera
            wakeCamera.transform.rotation = Quaternion.Slerp(r0, r1, k);   // rotate toward gameplay camera
            yield return null;
        }

        wakeCamera.enabled = false;                                      // switch off wake camera
        gameplayCamera.enabled = true;                                   // switch on gameplay camera
        wakeCamera.GetComponent<AudioListener>().enabled = false;        // transfer audio listener
        gameplayCamera.GetComponent<AudioListener>().enabled = true;
    }

    IEnumerator OpenEyes(float dur)
    {
        StartBlurPulse();
        yield return MoveLids(0f, 1f, dur); // animate from closed to open
    }

    void StartBlurPulse()
    {
        if (!blurOverlay) return;
        blurOverlay.alpha = blurStartAlpha;
        StartCoroutine(FadeCanvasGroup(blurOverlay, blurStartAlpha, 0f, blurFadeTime)); // fade blur out as eyes open
    }

    void SetLidsClosedInstant()
    {
        topLid.anchoredPosition = Vector2.zero;    // both lids at centre covering screen
        bottomLid.anchoredPosition = Vector2.zero;
    }

    IEnumerator MoveLids(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            ApplyLid01(Mathf.Lerp(from, to, lidCurve.Evaluate(t / dur))); // ease lids to target
            yield return null;
        }
        ApplyLid01(to);
    }

    void ApplyLid01(float v)
    {
        float m = Mathf.Lerp(0f, lidMove, v);
        topLid.anchoredPosition = new Vector2(0, m);     // top lid moves up
        bottomLid.anchoredPosition = new Vector2(0, -m); // bottom lid moves down
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float a, float b, float d)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(a, b, t / d); // gradually change alpha
            yield return null;
        }
        cg.alpha = b;
    }

    static void SetEnabled(MonoBehaviour[] list, bool v)
    {
        foreach (var m in list) if (m) m.enabled = v;
    }

    static void SetActive(GameObject[] list, bool v)
    {
        foreach (var g in list) if (g) g.SetActive(v);
    }
}