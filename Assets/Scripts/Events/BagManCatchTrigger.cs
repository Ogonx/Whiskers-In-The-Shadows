using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BagManCatchTrigger : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;
    public Transform bagManFaceTarget;          // the point the camera pans to during the jumpscare
    public Animator bagManAnimator;
    public GameObject bagManObject;             // BagMans root gameobject used to reset position
    public GameObject bagManRevealTrigger;      // reenabled on respawn so the forest reveal can fire again
    public GameObject bagManEscapeTrigger;      // reenabled on respawn
    public GameObject bagManWoodsRevealTriggerObject; // reenabled on respawn
    public BagManRevealDirector bagManRevealDirector;
    public BagManWoodsRevealDirector bagManWoodsRevealDirector;

    [Header("Audio")]
    public AudioSource jumpscareSource;
    public AudioClip jumpscareClip;
    public AudioSource chaseMusic;              // stopped immediately on catch
    public AudioSource windAudio;               // faded back in after respawn
    public AudioSource bagManFootsteps;         // stopped immediately on catch
    [Range(0f, 1f)] public float windResumeVolume = 1f; // volume to restore wind to after respawn
    public AudioSource[] audioSourcesToMute;    // any other sources to silence during the sequence

    [Header("Camera")]
    public float panDuration = 0.5f;   // how long the camera takes to pan to BagMans face
    public float panUpFOV = 20f;       // FOV to zoom into during the pan
    public float holdDuration = 0.5f;  // how long to hold on BagMans face before fading

    [Header("Fade")]
    public float fadeToBlackDuration = 0.5f;  // how long the screen takes to go black
    public float blackHoldDuration = 1.5f;    // how long to hold on black before reloading
    public float fadeBackInDuration = 1.5f;   // how long the fade back in takes after respawn

    bool triggered = false;      // stops the trigger firing more than once per encounter
    Vector3 bagManStartPos;      // saved at Start so BagMan can be reset to his original position
    Quaternion bagManStartRot;   // saved at Start so BagMan can be reset to his original rotation
    GameObject blackCanvas;      // reference to the black overlay so it can be destroyed on reload

    void Start()
    {
        // save BagMans starting transform so Reload can put him back
        if (bagManObject)
        {
            bagManStartPos = bagManObject.transform.position;
            bagManStartRot = bagManObject.transform.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;                   // already caught, ignore
        if (!other.CompareTag("Player")) return; // only the cat triggers this
        triggered = true;
        StartCoroutine(CatchSequence());
    }

    IEnumerator CatchSequence()
    {
        catController.FreezeMovement();          // stop the cat moving
        if (cameraFollow) cameraFollow.frozen = true; // lock the camera

        if (chaseMusic) chaseMusic.Stop();       // kill chase music immediately
        if (bagManFootsteps) bagManFootsteps.Stop(); // kill footstep audio

        foreach (var src in audioSourcesToMute)
            if (src) src.volume = 0f;            // silence any other audio sources

        yield return StartCoroutine(PanToBagManFace()); // pan camera to BagMan

        if (bagManAnimator) bagManAnimator.SetTrigger("Attack"); // play knife animation

        if (jumpscareSource && jumpscareClip)
            jumpscareSource.PlayOneShot(jumpscareClip); // play jumpscare sound

        yield return new WaitForSeconds(0.3f);   // brief pause before fading

        yield return StartCoroutine(FadeToBlack()); // fade screen to black

        yield return new WaitForSeconds(blackHoldDuration); // hold on black

        Reload(); // reset everything and respawn the cat
    }

    IEnumerator PanToBagManFace()
    {
        if (bagManFaceTarget == null) yield break; // nothing to pan to

        float elapsed = 0f;
        Quaternion startRot = mainCamera.transform.rotation; // save current camera rotation
        float startFOV = mainCamera.fieldOfView;             // save current FOV

        Vector3 dir = bagManFaceTarget.position - mainCamera.transform.position;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized); // work out rotation to face BagMan

        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / panDuration); // smooth interpolation value
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t); // rotate toward BagMan
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, panUpFOV, t);               // zoom in
            yield return null;
        }

        mainCamera.transform.rotation = targetRot; // snap to final rotation
        mainCamera.fieldOfView = panUpFOV;          // snap to final FOV

        yield return new WaitForSeconds(holdDuration); // hold on BagMans face
    }

    IEnumerator FadeToBlack()
    {
        // create a full-screen black Canvas on top of everything
        blackCanvas = new GameObject("CatchBlackCanvas");
        Canvas canvas = blackCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // on top of all other UI
        blackCanvas.AddComponent<CanvasScaler>();
        blackCanvas.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("BlackPanel");
        panelObj.transform.SetParent(blackCanvas.transform, false);
        Image img = panelObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; // stretch to fill screen
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f; // start transparent

        float elapsed = 0f;
        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeToBlackDuration); // fade to black
            yield return null;
        }
        cg.alpha = 1f; // make sure it's fully black
    }

    void Reload()
    {
        if (!CheckpointSystem.HasCheckpoint) return; // no checkpoint saved, do nothing

        CheckpointSystem.LoadCheckpoint(catController); // teleport cat to last checkpoint

        mainCamera.fieldOfView = 60f; // reset FOV back to normal

        if (cameraFollow)
        {
            cameraFollow.frozen = false;       // unfreeze camera
            cameraFollow.frontMode = false;    // back to normal follow mode
            cameraFollow.blendingBack = false; // cancel any blend
        }

        BagManRevealDirector.ChaseActive = false; // tell the rest of the game the chase is over

        if (bagManObject)
        {
            bagManObject.transform.position = bagManStartPos; // put BagMan back where he started
            bagManObject.transform.rotation = bagManStartRot;
        }

        if (bagManRevealDirector) bagManRevealDirector.ResetTrigger();           // reset forest reveal
        if (bagManWoodsRevealDirector) bagManWoodsRevealDirector.ResetTrigger(); // reset mansion reveal

        if (bagManAnimator)
        {
            bagManAnimator.SetBool("IsRunning", false); // stop run animation
            bagManAnimator.SetBool("IsWalking", false); // stop walk animation
        }

        if (bagManRevealTrigger) bagManRevealTrigger.SetActive(true);                     // reenabled reveal trigger
        if (bagManEscapeTrigger) bagManEscapeTrigger.SetActive(true);                     // reenable escape trigger
        if (bagManWoodsRevealTriggerObject) bagManWoodsRevealTriggerObject.SetActive(true); // reenable woods trigger

        catController.UnfreezeMovement(); // give control back to the player
        triggered = false;                // allow the catch to fire again

        StartCoroutine(FadeBackIn()); // fade the screen back in
    }

    IEnumerator FadeBackIn()
    {
        yield return new WaitForSeconds(0.1f); // tiny pause before fading

        if (blackCanvas != null) Destroy(blackCanvas); // destroy the old black canvas

        // create a new full-screen black canvas to fade out from
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
        cg.alpha = 1f; // start fully black

        float elapsed = 0f;
        while (elapsed < fadeBackInDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeBackInDuration); // fade from black to clear
            yield return null;
        }
        cg.alpha = 0f;
        Destroy(canvasObj); // clean up the canvas once done

        foreach (var src in audioSourcesToMute)
            if (src) src.volume = 1f; // restore all muted audio sources

        if (windAudio)
        {
            windAudio.volume = 0f;
            if (!windAudio.isPlaying) windAudio.Play(); // restart wind if it stopped
            StartCoroutine(FadeWindBack());              // fade wind back in gradually
        }
    }

    IEnumerator FadeWindBack()
    {
        float elapsed = 0f;
        float dur = 2f; // wind takes 2 seconds to fade back in
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            if (windAudio) windAudio.volume = Mathf.Lerp(0f, windResumeVolume, elapsed / dur); // raise wind volume
            yield return null;
        }
        if (windAudio) windAudio.volume = windResumeVolume; // make sure it lands on target
    }
}