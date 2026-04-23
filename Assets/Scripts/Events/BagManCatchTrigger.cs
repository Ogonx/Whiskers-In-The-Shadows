using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BagManCatchTrigger : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform bagManFaceTarget;
    public Animator bagManAnimator;
    public GameObject bagManObject;
    public GameObject bagManRevealTrigger;
    public GameObject bagManEscapeTrigger;
    public GameObject bagManWoodsRevealTriggerObject;
    public BagManRevealDirector bagManRevealDirector;
    public BagManWoodsRevealDirector bagManWoodsRevealDirector;

    [Header("Audio")]
    public AudioSource jumpscareSource;
    public AudioClip jumpscareClip;
    public AudioSource chaseMusic;
    public AudioSource windAudio;
    public AudioSource bagManFootsteps;
    [Range(0f, 1f)] public float windResumeVolume = 1f;
    public AudioSource[] audioSourcesToMute;

    [Header("Camera")]
    public float panDuration = 0.5f;
    public float panUpFOV = 20f;
    public float holdDuration = 0.5f;

    [Header("Fade")]
    public float fadeToBlackDuration = 0.5f;
    public float blackHoldDuration = 1.5f;
    public float fadeBackInDuration = 1.5f;

    bool triggered = false;
    Vector3 bagManStartPos;
    Quaternion bagManStartRot;
    GameObject blackCanvas;

    void Start()
    {
        if (bagManObject)
        {
            bagManStartPos = bagManObject.transform.position;
            bagManStartRot = bagManObject.transform.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(CatchSequence());
    }

    IEnumerator CatchSequence()
    {
        catController.FreezeMovement();
        if (cameraFollow) cameraFollow.frozen = true;

        if (chaseMusic) chaseMusic.Stop();
        if (bagManFootsteps) bagManFootsteps.Stop();

        foreach (var src in audioSourcesToMute)
            if (src) src.volume = 0f;

        yield return StartCoroutine(PanToBagManFace());

        if (bagManAnimator) bagManAnimator.SetTrigger("Attack");

        if (jumpscareSource && jumpscareClip)
            jumpscareSource.PlayOneShot(jumpscareClip);

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(FadeToBlack());

        yield return new WaitForSeconds(blackHoldDuration);

        Reload();
    }

    IEnumerator PanToBagManFace()
    {
        if (bagManFaceTarget == null) yield break;

        float elapsed = 0f;
        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        Vector3 dir = bagManFaceTarget.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, panUpFOV, t);
            yield return null;
        }

        mainCamera.transform.rotation = targetRot;
        mainCamera.fieldOfView = panUpFOV;

        yield return new WaitForSeconds(holdDuration);
    }

    IEnumerator FadeToBlack()
    {
        blackCanvas = new GameObject("CatchBlackCanvas");
        Canvas canvas = blackCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        blackCanvas.AddComponent<CanvasScaler>();
        blackCanvas.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("BlackPanel");
        panelObj.transform.SetParent(blackCanvas.transform, false);
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
        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeToBlackDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    void Reload()
    {
        if (!CheckpointSystem.HasCheckpoint) return;

        CheckpointSystem.LoadCheckpoint(catController);

        mainCamera.fieldOfView = 60f;

        if (cameraFollow)
        {
            cameraFollow.frozen = false;
            cameraFollow.frontMode = false;
            cameraFollow.blendingBack = false;
        }

        BagManRevealDirector.ChaseActive = false;

        if (bagManObject)
        {
            bagManObject.transform.position = bagManStartPos;
            bagManObject.transform.rotation = bagManStartRot;
        }

        if (bagManRevealDirector) bagManRevealDirector.ResetTrigger();
        if (bagManWoodsRevealDirector) bagManWoodsRevealDirector.ResetTrigger();

        if (bagManAnimator)
        {
            bagManAnimator.SetBool("IsRunning", false);
            bagManAnimator.SetBool("IsWalking", false);
        }

        if (bagManRevealTrigger) bagManRevealTrigger.SetActive(true);
        if (bagManEscapeTrigger) bagManEscapeTrigger.SetActive(true);
        if (bagManWoodsRevealTriggerObject) bagManWoodsRevealTriggerObject.SetActive(true);

        catController.UnfreezeMovement();
        triggered = false;

        StartCoroutine(FadeBackIn());
    }

    IEnumerator FadeBackIn()
    {
        yield return new WaitForSeconds(0.1f);

        if (blackCanvas != null) Destroy(blackCanvas);

        GameObject canvasObj = new GameObject("ReloadFadeCanvas");
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

        float elapsed = 0f;
        while (elapsed < fadeBackInDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeBackInDuration);
            yield return null;
        }
        cg.alpha = 0f;
        Destroy(canvasObj);

        foreach (var src in audioSourcesToMute)
            if (src) src.volume = 1f;

        if (windAudio)
        {
            windAudio.volume = 0f;
            if (!windAudio.isPlaying) windAudio.Play();
            StartCoroutine(FadeWindBack());
        }
    }

    IEnumerator FadeWindBack()
    {
        float elapsed = 0f;
        float dur = 2f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            if (windAudio) windAudio.volume = Mathf.Lerp(0f, windResumeVolume, elapsed / dur);
            yield return null;
        }
        if (windAudio) windAudio.volume = windResumeVolume;
    }
}