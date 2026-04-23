using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SleepEndingDirector : MonoBehaviour
{
    [Header("Bed Interaction")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Transform bedTransform;
    public float interactRange = 2f;

    [Header("UI Prompt")]
    public GameObject promptRoot;
    public TextMeshProUGUI promptText;
    public string promptMessage = "Press E to sleep";

    [Header("Cameras")]
    public Camera wakeCamera;
    public Camera gameplayCamera;
    public GameObject cutsceneUIRoot;
    public Transform sleepCamPosition;

    [Header("Eyelids")]
    public RectTransform topLid;
    public RectTransform bottomLid;
    public float openGapY = 18f;
    public float lidOverscan = 40f;
    public AnimationCurve lidCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Blur Overlay")]
    public CanvasGroup blurOverlay;
    [Range(0f, 1f)] public float blurStartAlpha = 0.55f;
    public float blurFadeTime = 1.1f;

    [Header("Person")]
    public GameObject ownerObject;
    public Transform ownerWalkStart;
    public Transform ownerStopPoint1;
    public Transform ownerStopPoint2;
    public float ownerWalkSpeed = 2.5f;
    public float rotateSpeed = 540f;
    public float arriveDistance = 0.05f;
    public float stopLookHold = 0.9f;

    [Header("Animator")]
    public Animator personAnimator;
    public string walkingBoolName = "IsWalking";

    [Header("BagMan")]
    public GameObject bagManObject;
    public Animator bagManAnimator;
    public Transform bagManFaceTarget;

    [Header("Audio")]
    public AudioSource windSource;
    public float windFadeInDuration = 3f;
    [Range(0f, 1f)] public float windTargetVolume = 0.8f;
    public AudioSource footstepSource;
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepsVolume = 0.8f;
    public AudioSource glitchSource;
    public AudioClip glitchClip;
    public AudioSource jumpscareSource;
    public AudioClip jumpscareClip;

    [Header("Timing")]
    public float blackHoldTime = 4f;
    public float openEyesTime = 1.2f;
    public float footstepFadeInTime = 2f;
    public float footstepHoldTime = 2f;
    public float glitch1HoldTime = 2f;

    [Header("Pan To Face")]
    public float panUpDuration = 0.5f;
    public float panUpFOV = 20f;
    public float panUpHoldTime = 0.5f;

    [Header("Fade")]
    public float fadeOutDuration = 2f;
    public float fadeToBlackDuration = 2f;

    bool triggered = false;
    bool unlocked = false;
    float lidMove;

    void Start()
    {
        if (promptRoot) promptRoot.SetActive(false);
        if (promptText) promptText.text = promptMessage;
        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject) bagManObject.SetActive(false);
    }

    public void Unlock()
    {
        unlocked = true;
    }

    void Update()
    {
        if (triggered || !unlocked) return;

        float dist = Vector3.Distance(catController.transform.position, bedTransform.position);
        bool inRange = dist <= interactRange;

        if (promptRoot) promptRoot.SetActive(inRange);

        if (inRange && catController.ConsumeInteractPressed())
        {
            triggered = true;
            if (promptRoot) promptRoot.SetActive(false);
            StartCoroutine(SleepSequence());
        }
    }

    IEnumerator SleepSequence()
    {
        catController.FreezeMovement();
        cameraFollow.frozen = true;

        yield return StartCoroutine(FadeToBlackAndAudio());

        if (sleepCamPosition && wakeCamera)
        {
            wakeCamera.transform.position = sleepCamPosition.position;
            wakeCamera.transform.rotation = sleepCamPosition.rotation;
        }

        if (cutsceneUIRoot) cutsceneUIRoot.SetActive(true);

        float lidHeight = Mathf.Max(topLid.rect.height, bottomLid.rect.height);
        if (lidHeight < 10f) lidHeight = 600f;
        lidMove = lidHeight + lidOverscan + (openGapY * 0.5f);

        SetLidsClosedInstant();
        if (blurOverlay) blurOverlay.alpha = 0f;

        if (wakeCamera) wakeCamera.enabled = true;
        if (gameplayCamera) gameplayCamera.enabled = false;

        yield return new WaitForSeconds(blackHoldTime);

        yield return StartCoroutine(FadeInWind());
        yield return StartCoroutine(OpenEyes(openEyesTime));

        if (footstepSource && footstepClip)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.volume = 0f;
            footstepSource.Play();
            AudioListener.volume = 1f;
            yield return StartCoroutine(FadeAudio(footstepSource, 0f, footstepsVolume, footstepFadeInTime));
        }

        yield return new WaitForSeconds(footstepHoldTime);

        if (ownerObject)
        {
            ownerObject.SetActive(true);
            if (ownerWalkStart) ownerObject.transform.position = ownerWalkStart.position;

            yield return StartCoroutine(WalkTo(ownerStopPoint1, true));
            yield return StartCoroutine(GlitchAtPoint1());
            yield return new WaitForSeconds(stopLookHold);

            yield return StartCoroutine(WalkTo(ownerStopPoint2, false));
            yield return StartCoroutine(GlitchAtPoint2());
        }

        if (footstepSource) footstepSource.Stop();
    }

    IEnumerator GlitchAtPoint1()
    {
        if (footstepSource) footstepSource.Pause();

        StartCoroutine(GlitchEffect());

        if (glitchSource && glitchClip)
            glitchSource.PlayOneShot(glitchClip);

        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator)
            {
                bagManAnimator.SetBool("IsRunning", false);
                bagManAnimator.SetBool("IsWalking", false);
            }
        }

        yield return new WaitForSeconds(glitch1HoldTime);

        StartCoroutine(GlitchEffect());

        if (bagManObject) bagManObject.SetActive(false);
        if (ownerObject) ownerObject.SetActive(true);

        if (footstepSource) footstepSource.UnPause();
    }

    IEnumerator GlitchAtPoint2()
    {
        if (footstepSource) footstepSource.Pause();

        StartCoroutine(GlitchEffect());

        if (glitchSource && glitchClip)
            glitchSource.PlayOneShot(glitchClip);

        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator)
            {
                bagManAnimator.SetBool("IsRunning", false);
                bagManAnimator.SetBool("IsWalking", false);
            }
        }

        yield return new WaitForSeconds(0.3f);

        StartCoroutine(GlitchEffect());
        if (bagManObject) bagManObject.SetActive(false);
        if (ownerObject) ownerObject.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        StartCoroutine(GlitchEffect());

        if (glitchSource && glitchClip)
            glitchSource.PlayOneShot(glitchClip);

        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator)
            {
                bagManAnimator.SetBool("IsRunning", false);
                bagManAnimator.SetBool("IsWalking", false);
            }
        }

        yield return new WaitForSeconds(0.2f);

        StartCoroutine(GlitchEffect());
        if (bagManObject) bagManObject.SetActive(false);
        if (ownerObject) ownerObject.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        StartCoroutine(GlitchEffect());

        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator)
            {
                bagManAnimator.SetBool("IsRunning", false);
                bagManAnimator.SetBool("IsWalking", false);
            }
        }

        yield return StartCoroutine(PanUpToBagManFace());

        if (bagManAnimator) bagManAnimator.SetTrigger("Attack");

        if (jumpscareSource && jumpscareClip)
            jumpscareSource.PlayOneShot(jumpscareClip);

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(InstantBlack());

        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator InstantBlack()
    {
        GameObject canvasObj = new GameObject("InstantBlackCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("BlackPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image img = panelObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator GlitchEffect()
    {
        GameObject flashObj = new GameObject("GlitchFlash");
        Canvas flashCanvas = flashObj.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 1000;
        flashObj.AddComponent<CanvasScaler>();
        flashObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("GlitchPanel");
        panelObj.transform.SetParent(flashObj.transform, false);
        Image img = panelObj.AddComponent<Image>();
        img.color = Color.white;
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0.6f;

        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0.6f, 0f, elapsed / duration);
            yield return null;
        }

        cg.alpha = 0f;
        Destroy(flashObj);
    }

    IEnumerator PanUpToBagManFace()
    {
        if (wakeCamera == null || bagManFaceTarget == null) yield break;

        float elapsed = 0f;
        Quaternion startRot = wakeCamera.transform.rotation;
        float startFOV = wakeCamera.fieldOfView;

        Vector3 dir = bagManFaceTarget.position - wakeCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        while (elapsed < panUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panUpDuration);
            wakeCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            wakeCamera.fieldOfView = Mathf.Lerp(startFOV, panUpFOV, t);
            yield return null;
        }

        wakeCamera.transform.rotation = targetRot;
        wakeCamera.fieldOfView = panUpFOV;

        yield return new WaitForSeconds(panUpHoldTime);
    }

    IEnumerator FadeInWind()
    {
        if (windSource == null) yield break;
        windSource.volume = 0f;
        windSource.Play();
        AudioListener.volume = 1f;
        float elapsed = 0f;
        while (elapsed < windFadeInDuration)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(0f, windTargetVolume, elapsed / windFadeInDuration);
            yield return null;
        }
        windSource.volume = windTargetVolume;
    }

    IEnumerator FadeToBlackAndAudio()
    {
        GameObject canvasObj = new GameObject("SleepBlackCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 997;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("BlackPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image img = panelObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        float elapsed = 0f;
        float startVol = AudioListener.volume;

        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeToBlackDuration;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            AudioListener.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        cg.alpha = 1f;
        AudioListener.volume = 0f;
        Destroy(canvasObj);
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
        if (topLid) topLid.anchoredPosition = Vector2.zero;
        if (bottomLid) bottomLid.anchoredPosition = Vector2.zero;
    }

    IEnumerator MoveLids(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            ApplyLid01(Mathf.Lerp(from, to, lidCurve.Evaluate(Mathf.Clamp01(t / dur))));
            yield return null;
        }
        ApplyLid01(to);
    }

    void ApplyLid01(float v)
    {
        float m = Mathf.Lerp(0f, lidMove, v);
        if (topLid) topLid.anchoredPosition = new Vector2(0, m);
        if (bottomLid) bottomLid.anchoredPosition = new Vector2(0, -m);
    }

    IEnumerator WalkTo(Transform target, bool faceCamera)
    {
        if (!ownerObject || !target) yield break;

        SetWalking(true);

        while (Vector3.Distance(ownerObject.transform.position, target.position) > arriveDistance)
        {
            ownerObject.transform.position = Vector3.MoveTowards(ownerObject.transform.position, target.position, ownerWalkSpeed * Time.deltaTime);
            Vector3 dir = target.position - ownerObject.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                ownerObject.transform.rotation = Quaternion.RotateTowards(ownerObject.transform.rotation, Quaternion.LookRotation(dir.normalized), rotateSpeed * Time.deltaTime);
            yield return null;
        }

        SetWalking(false);

        if (faceCamera && wakeCamera)
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                Vector3 dir = wakeCamera.transform.position - ownerObject.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    ownerObject.transform.rotation = Quaternion.RotateTowards(ownerObject.transform.rotation, Quaternion.LookRotation(dir.normalized), rotateSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    void SetWalking(bool walking)
    {
        if (personAnimator) personAnimator.SetBool(walkingBoolName, walking);
    }

    IEnumerator FadeToBlack()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("BlackPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image img = panelObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    IEnumerator FadeAudio(AudioSource src, float from, float to, float dur)
    {
        if (!src) yield break;
        float t = 0f;
        src.volume = from;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        src.volume = to;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        if (!cg) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        cg.alpha = to;
    }
}