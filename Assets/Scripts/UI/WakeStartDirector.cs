using System.Collections;
using UnityEngine;

public class WakeStartDirector : MonoBehaviour
{
    [Header("Run Control")]
    [SerializeField] bool requireWakeFlag = true;

    [Header("Cutscene UI Root")]
    [SerializeField] GameObject cutsceneUIRoot;

    [Header("Eyelids")]
    [SerializeField] RectTransform topLid;
    [SerializeField] RectTransform bottomLid;
    [SerializeField] float openGapY = 18f;
    [SerializeField] float lidOverscan = 40f;
    [SerializeField] AnimationCurve lidCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Blur Overlay")]
    [SerializeField] CanvasGroup blurOverlay;
    [Range(0f, 1f)][SerializeField] float blurStartAlpha = 0.55f;
    [SerializeField] float blurFadeTime = 1.1f;

    [Header("Cameras")]
    [SerializeField] Camera wakeCamera;
    [SerializeField] Camera gameplayCamera;
    [SerializeField] float cameraBlendTime = 1.2f;

    [Header("Hold After Eyes Open")]
    [SerializeField] float holdAfterEyesOpenSeconds = 3f;

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
    [SerializeField] float holdBlackSeconds = 4f;
    [SerializeField] float bangDelayAfterBlack = 0.6f;
    [SerializeField] float openEyesTime = 1.2f;

    [Header("On Wake Enable")]
    [SerializeField] MonoBehaviour[] scriptsToEnable;
    [SerializeField] GameObject[] objectsToEnable;

    float lidMove;

    void Start()
    {
        if (requireWakeFlag && !WakeState.PlayWakeSequenceOnLoad) return;
        WakeState.PlayWakeSequenceOnLoad = false;

        SetEnabled(scriptsToEnable, false);
        SetActive(objectsToEnable, false);

        SetupCameras();
        SetupUI();

        StartCoroutine(WakeRoutine());
    }

    void SetupCameras()
    {
        wakeCamera.enabled = true;
        gameplayCamera.enabled = false;

        if (!wakeCamera.GetComponent<AudioListener>())
            wakeCamera.gameObject.AddComponent<AudioListener>();

        if (!gameplayCamera.GetComponent<AudioListener>())
            gameplayCamera.gameObject.AddComponent<AudioListener>();
    }

    void SetupUI()
    {
        float lidHeight = Mathf.Max(topLid.rect.height, bottomLid.rect.height);
        if (lidHeight < 10f) lidHeight = 600f;

        lidMove = lidHeight + lidOverscan + (openGapY * 0.5f);

        SetLidsClosedInstant();

        if (blurOverlay) blurOverlay.alpha = 0f;
    }

    IEnumerator WakeRoutine()
    {
        yield return new WaitForSecondsRealtime(holdBlackSeconds);
        yield return new WaitForSecondsRealtime(bangDelayAfterBlack);

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

        Coroutine eyes = StartCoroutine(OpenEyes(openEyesTime));
        Coroutine shake = enableDoorShake ? StartCoroutine(DoorImpactShake(wakeCamera.transform, shakeDuration)) : null;

        yield return eyes;
        if (shake != null) yield return shake;

        yield return new WaitForSecondsRealtime(holdAfterEyesOpenSeconds);
        yield return BlendToGameplayCamera();

        if (cutsceneUIRoot) cutsceneUIRoot.SetActive(false);

        SetActive(objectsToEnable, true);
        SetEnabled(scriptsToEnable, true);
    }

    IEnumerator DoorImpactShake(Transform cam, float dur)
    {
        Vector3 baseRot = cam.localEulerAngles;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float fade = 1f - t / dur;
            float wob = Mathf.Sin(t * shakeSpeed) * fade;
            cam.localEulerAngles = new Vector3(baseRot.x + wob * shakePitch, baseRot.y, baseRot.z + wob * shakeRoll);
            yield return null;
        }

        cam.localEulerAngles = baseRot;
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
            wakeCamera.transform.position = Vector3.Lerp(p0, p1, k);
            wakeCamera.transform.rotation = Quaternion.Slerp(r0, r1, k);
            yield return null;
        }

        wakeCamera.enabled = false;
        gameplayCamera.enabled = true;
        wakeCamera.GetComponent<AudioListener>().enabled = false;
        gameplayCamera.GetComponent<AudioListener>().enabled = true;
    }

    IEnumerator OpenEyes(float dur)
    {
        StartBlurPulse();
        yield return MoveLids(0f, 1f, dur);
    }

    void StartBlurPulse()
    {
        if (!blurOverlay) return;
        blurOverlay.alpha = blurStartAlpha;
        StartCoroutine(FadeCanvasGroup(blurOverlay, blurStartAlpha, 0f, blurFadeTime));
    }

    void SetLidsClosedInstant()
    {
        topLid.anchoredPosition = Vector2.zero;
        bottomLid.anchoredPosition = Vector2.zero;
    }

    IEnumerator MoveLids(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            ApplyLid01(Mathf.Lerp(from, to, lidCurve.Evaluate(t / dur)));
            yield return null;
        }
        ApplyLid01(to);
    }

    void ApplyLid01(float v)
    {
        float m = Mathf.Lerp(0f, lidMove, v);
        topLid.anchoredPosition = new Vector2(0, m);
        bottomLid.anchoredPosition = new Vector2(0, -m);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float a, float b, float d)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(a, b, t / d);
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