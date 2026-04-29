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
    public Transform bedTransform;   // the bed the player interacts with to trigger the ending
    public float interactRange = 2f; // how close the cat needs to be to interact

    [Header("UI Prompt")]
    public GameObject promptRoot;
    public TextMeshProUGUI promptText;
    public string promptMessage = "Press E to sleep";

    [Header("Cameras")]
    public Camera wakeCamera;        // the cutscene camera used during the ending sequence
    public Camera gameplayCamera;
    public GameObject cutsceneUIRoot;
    public Transform sleepCamPosition; // where the wake camera is positioned at the start

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
    public GameObject ownerObject;       // the owner character that walks in
    public Transform ownerWalkStart;     // where the owner starts
    public Transform ownerStopPoint1;    // first stop where glitch 1 happens
    public Transform ownerStopPoint2;    // second stop where glitch 2 and the ending happens
    public float ownerWalkSpeed = 2.5f;
    public float rotateSpeed = 540f;
    public float arriveDistance = 0.05f; // how close the owner needs to get to count as arrived
    public float stopLookHold = 0.9f;    // how long to hold after the owner faces the camera

    [Header("Animator")]
    public Animator personAnimator;
    public string walkingBoolName = "IsWalking";

    [Header("BagMan")]
    public GameObject bagManObject;    // replaces the owner during glitch sequences
    public Animator bagManAnimator;
    public Transform bagManFaceTarget; // camera target for the final pan up

    [Header("Audio")]
    public AudioSource windSource;
    public float windFadeInDuration = 3f;
    [Range(0f, 1f)] public float windTargetVolume = 0.8f;
    public AudioSource footstepSource;   // the approaching footstep audio
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepsVolume = 0.8f;
    public AudioSource glitchSource;     // plays a sound effect with each glitch flash
    public AudioClip glitchClip;
    public AudioSource jumpscareSource;  // final jumpscare audio sting
    public AudioClip jumpscareClip;

    [Header("Timing")]
    public float blackHoldTime = 4f;       // how long to hold on black before opening eyes
    public float openEyesTime = 1.2f;      // how long the eyelid open animation takes
    public float footstepFadeInTime = 2f;  // how long footsteps take to fade in
    public float footstepHoldTime = 2f;    // how long to listen to footsteps before owner appears
    public float glitch1HoldTime = 2f;     // how long BagMan is visible during the first glitch

    [Header("Pan To Face")]
    public float panUpDuration = 0.5f;    // how long the final camera pan takes
    public float panUpFOV = 20f;          // FOV for the final zoom on BagMan
    public float panUpHoldTime = 0.5f;    // how long to hold on BagMan before the knife attack

    [Header("Fade")]
    public float fadeOutDuration = 2f;
    public float fadeToBlackDuration = 2f;

    bool triggered = false;
    bool unlocked = false; // set to true by HomeReturnDirector when the player gets home
    float lidMove;         // calculated lid travel distance based on screen size

    void Start()
    {
        if (promptRoot) promptRoot.SetActive(false);
        if (promptText) promptText.text = promptMessage;
        if (ownerObject) ownerObject.SetActive(false);   // owner starts hidden
        if (bagManObject) bagManObject.SetActive(false); // BagMan starts hidden
    }

    public void Unlock()
    {
        unlocked = true; // called by HomeReturnDirector to enable the bed interaction
    }

    void Update()
    {
        if (triggered || !unlocked) return; // do nothing until unlocked and not already triggered

        float dist = Vector3.Distance(catController.transform.position, bedTransform.position);
        bool inRange = dist <= interactRange;

        if (promptRoot) promptRoot.SetActive(inRange); // show prompt when close to bed

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

        yield return StartCoroutine(FadeToBlackAndAudio()); // fade screen and audio to black

        // position the wake camera for the opening shot
        if (sleepCamPosition && wakeCamera)
        {
            wakeCamera.transform.position = sleepCamPosition.position;
            wakeCamera.transform.rotation = sleepCamPosition.rotation;
        }

        if (cutsceneUIRoot) cutsceneUIRoot.SetActive(true);

        // calculate how far the lids need to move based on screen size
        float lidHeight = Mathf.Max(topLid.rect.height, bottomLid.rect.height);
        if (lidHeight < 10f) lidHeight = 600f;
        lidMove = lidHeight + lidOverscan + (openGapY * 0.5f);

        SetLidsClosedInstant();
        if (blurOverlay) blurOverlay.alpha = 0f;

        if (wakeCamera) wakeCamera.enabled = true;     // switch to wake camera
        if (gameplayCamera) gameplayCamera.enabled = false;

        yield return new WaitForSeconds(blackHoldTime); // hold on black before opening eyes

        yield return StartCoroutine(FadeInWind());
        yield return StartCoroutine(OpenEyes(openEyesTime)); // open the eyelid panels

        // fade in footstep audio to build tension
        if (footstepSource && footstepClip)
        {
            footstepSource.clip = footstepClip;
            footstepSource.loop = true;
            footstepSource.volume = 0f;
            footstepSource.Play();
            AudioListener.volume = 1f;
            yield return StartCoroutine(FadeAudio(footstepSource, 0f, footstepsVolume, footstepFadeInTime));
        }

        yield return new WaitForSeconds(footstepHoldTime); // hold on footsteps before owner appears

        if (ownerObject)
        {
            ownerObject.SetActive(true);
            if (ownerWalkStart) ownerObject.transform.position = ownerWalkStart.position;

            yield return StartCoroutine(WalkTo(ownerStopPoint1, true));  // walk to first stop
            yield return StartCoroutine(GlitchAtPoint1());               // first glitch sequence
            yield return new WaitForSeconds(stopLookHold);

            yield return StartCoroutine(WalkTo(ownerStopPoint2, false)); // walk to second stop
            yield return StartCoroutine(GlitchAtPoint2());               // second escalating glitch into ending
        }

        if (footstepSource) footstepSource.Stop();
    }

    IEnumerator GlitchAtPoint1()
    {
        if (footstepSource) footstepSource.Pause();

        StartCoroutine(GlitchEffect()); // white flash

        if (glitchSource && glitchClip) glitchSource.PlayOneShot(glitchClip);

        // swap owner for BagMan
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

        yield return new WaitForSeconds(glitch1HoldTime); // hold BagMan visible

        StartCoroutine(GlitchEffect()); // flash back to owner

        if (bagManObject) bagManObject.SetActive(false);
        if (ownerObject) ownerObject.SetActive(true);

        if (footstepSource) footstepSource.UnPause();
    }

    IEnumerator GlitchAtPoint2()
    {
        // escalating glitch sequence with increasing frequency until BagMan locks on permanently
        if (footstepSource) footstepSource.Pause();

        StartCoroutine(GlitchEffect());
        if (glitchSource && glitchClip) glitchSource.PlayOneShot(glitchClip);

        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator) { bagManAnimator.SetBool("IsRunning", false); bagManAnimator.SetBool("IsWalking", false); }
        }

        yield return new WaitForSeconds(0.3f); // shorter hold this time

        StartCoroutine(GlitchEffect());
        if (bagManObject) bagManObject.SetActive(false);
        if (ownerObject) ownerObject.SetActive(true);

        yield return new WaitForSeconds(0.2f); // even shorter

        StartCoroutine(GlitchEffect());
        if (glitchSource && glitchClip) glitchSource.PlayOneShot(glitchClip);

        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator) { bagManAnimator.SetBool("IsRunning", false); bagManAnimator.SetBool("IsWalking", false); }
        }

        yield return new WaitForSeconds(0.2f);

        StartCoroutine(GlitchEffect());
        if (bagManObject) bagManObject.SetActive(false);
        if (ownerObject) ownerObject.SetActive(true);

        yield return new WaitForSeconds(0.1f); // now very fast

        StartCoroutine(GlitchEffect());

        // final swap to BagMan permanently
        if (ownerObject) ownerObject.SetActive(false);
        if (bagManObject)
        {
            bagManObject.SetActive(true);
            bagManObject.transform.position = ownerObject.transform.position;
            bagManObject.transform.rotation = ownerObject.transform.rotation;
            if (bagManAnimator) { bagManAnimator.SetBool("IsRunning", false); bagManAnimator.SetBool("IsWalking", false); }
        }

        yield return StartCoroutine(PanUpToBagManFace()); // pan camera up to BagMan's face

        if (bagManAnimator) bagManAnimator.SetTrigger("Attack"); // play knife animation

        if (jumpscareSource && jumpscareClip) jumpscareSource.PlayOneShot(jumpscareClip); // jumpscare sting

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(InstantBlack()); // instant black screen

        SceneManager.LoadScene("MainMenu"); // return to main menu
    }

    IEnumerator InstantBlack()
    {
        // create a full-screen black canvas instantly
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
        cg.alpha = 1f; // fully black immediately

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator GlitchEffect()
    {
        // creates a brief white full-screen flash that fades out over 0.15 seconds
        GameObject flashObj = new GameObject("GlitchFlash");
        Canvas flashCanvas = flashObj.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 1000; // on top of everything
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
            cg.alpha = Mathf.Lerp(0.6f, 0f, elapsed / duration); // fade white flash out
            yield return null;
        }

        cg.alpha = 0f;
        Destroy(flashObj); // clean up after the flash
    }

    IEnumerator PanUpToBagManFace()
    {
        if (wakeCamera == null || bagManFaceTarget == null) yield break;

        float elapsed = 0f;
        Quaternion startRot = wakeCamera.transform.rotation;
        float startFOV = wakeCamera.fieldOfView;

        Vector3 dir = bagManFaceTarget.position - wakeCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized); // face BagMan

        while (elapsed < panUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panUpDuration);
            wakeCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t); // pan to BagMan
            wakeCamera.fieldOfView = Mathf.Lerp(startFOV, panUpFOV, t);               // zoom in
            yield return null;
        }

        wakeCamera.transform.rotation = targetRot;
        wakeCamera.fieldOfView = panUpFOV;

        yield return new WaitForSeconds(panUpHoldTime); // hold on BagMan's face
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
            windSource.volume = Mathf.Lerp(0f, windTargetVolume, elapsed / windFadeInDuration); // fade wind in
            yield return null;
        }
        windSource.volume = windTargetVolume;
    }

    IEnumerator FadeToBlackAndAudio()
    {
        // creates a black canvas and fades screen and audio to black simultaneously
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
            cg.alpha = Mathf.Lerp(0f, 1f, t);              // fade screen to black
            AudioListener.volume = Mathf.Lerp(startVol, 0f, t); // fade all audio out
            yield return null;
        }

        cg.alpha = 1f;
        AudioListener.volume = 0f;
        Destroy(canvasObj);
    }

    IEnumerator OpenEyes(float dur)
    {
        StartBlurPulse();
        yield return MoveLids(0f, 1f, dur); // animate lids from closed to open
    }

    void StartBlurPulse()
    {
        if (!blurOverlay) return;
        blurOverlay.alpha = blurStartAlpha;
        StartCoroutine(FadeCanvasGroup(blurOverlay, blurStartAlpha, 0f, blurFadeTime)); // fade blur out
    }

    void SetLidsClosedInstant()
    {
        if (topLid) topLid.anchoredPosition = Vector2.zero;    // top lid at centre
        if (bottomLid) bottomLid.anchoredPosition = Vector2.zero; // bottom lid at centre
    }

    IEnumerator MoveLids(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // use unscaled time so it works if time scale is changed
            ApplyLid01(Mathf.Lerp(from, to, lidCurve.Evaluate(Mathf.Clamp01(t / dur))));
            yield return null;
        }
        ApplyLid01(to);
    }

    void ApplyLid01(float v)
    {
        float m = Mathf.Lerp(0f, lidMove, v);
        if (topLid) topLid.anchoredPosition = new Vector2(0, m);    // move top lid up
        if (bottomLid) bottomLid.anchoredPosition = new Vector2(0, -m); // move bottom lid down
    }

    IEnumerator WalkTo(Transform target, bool faceCamera)
    {
        if (!ownerObject || !target) yield break;

        SetWalking(true);

        // move owner toward target
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

        // optionally rotate owner to face the camera
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
        if (personAnimator) personAnimator.SetBool(walkingBoolName, walking); // toggle walk animation
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
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration); // fade to black
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
            src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur)); // gradually change volume
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
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur)); // fade canvas group alpha
            yield return null;
        }
        cg.alpha = to;
    }
}