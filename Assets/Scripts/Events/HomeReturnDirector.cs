using UnityEngine;
using System.Collections;

public class HomeReturnDirector : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;

    [Header("Cat Path")]
    public Transform[] waypoints; // the path the cat walks along automatically
    public float walkSpeed = 2f;

    [Header("Teleport")]
    public Transform homeSpawnPoint; // where the cat teleports to after the fade to black

    [Header("Audio")]
    public AudioSource windAudio;
    public float audioFadeDuration = 2f;

    [Header("Music")]
    public AudioSource ambientMusicSource;
    public AudioClip ambientMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    public float musicFadeInDuration = 2f;

    [Header("Camera")]
    public float targetDistance = 12f; // how far the camera pulls back during the cinematic
    public float targetHeight = 8f;    // how high the camera rises during the cinematic
    public float riseDuration = 4f;    // how long the camera takes to reach the zoomed-out position

    [Header("Fade")]
    public float fadeOutDuration = 2f;
    public float fadeInDuration = 2f;

    [Header("Trail")]
    public ScentTrail houseTrail; // activated after teleport to guide the player inside

    [Header("Ending")]
    public SleepEndingDirector sleepEndingDirector; // unlocked at the end of this sequence

    bool triggered = false;
    CanvasGroup fadeCanvas; // the black overlay used for the transition

    void Start()
    {
        // create a persistent black canvas for fade transitions
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image img = panelObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeCanvas = panelObj.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f; // start transparent
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(HomeReturnSequence());
    }

    IEnumerator HomeReturnSequence()
    {
        catController.FreezeMovement(); // stop the player controlling the cat

        // reset camera state before the cinematic
        cameraFollow.frozen = false;
        cameraFollow.blendingBack = false;
        cameraFollow.frontMode = false;

        if (windAudio) windAudio.volume = 0f;
        if (windAudio) windAudio.Stop();

        StartCoroutine(ZoomOut());      // pull camera back to cinematic height
        StartCoroutine(FadeInMusic());  // start ambient music

        yield return StartCoroutine(WalkCatAlongPath()); // walk cat home automatically

        yield return StartCoroutine(FadeOutAll()); // fade screen and audio to black

        // teleport cat to house interior
        catController.transform.position = homeSpawnPoint.position;
        catController.transform.rotation = homeSpawnPoint.rotation;

        // reset camera to close follow distance for inside the house
        cameraFollow.distance = 4f;
        cameraFollow.height = 2f;

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeIn()); // fade back in from black

        if (windAudio)
        {
            windAudio.volume = 0f;
            windAudio.Play();
            yield return StartCoroutine(FadeInWind()); // fade wind back in
        }

        if (houseTrail) houseTrail.UnlockAndShow(); // show the trail leading inside

        if (sleepEndingDirector) sleepEndingDirector.Unlock(); // allow the sleep ending to trigger

        catController.UnfreezeMovement(); // give control back to player

        gameObject.SetActive(false); // disable so this sequence wont fire again
    }

    IEnumerator WalkCatAlongPath()
    {
        Rigidbody rb = catController.GetComponent<Rigidbody>();
        Animator animator = catController.GetComponent<Animator>();

        foreach (Transform waypoint in waypoints)
        {
            while (Vector3.Distance(catController.transform.position, waypoint.position) > 0.5f)
            {
                Vector3 dir = (waypoint.position - catController.transform.position);
                dir.y = 0f;
                dir.Normalize();

                if (rb)
                {
                    Vector3 vel = rb.linearVelocity;
                    vel.x = dir.x * walkSpeed;
                    vel.z = dir.z * walkSpeed;
                    rb.linearVelocity = vel; // move via rigidbody velocity
                }

                if (dir.sqrMagnitude > 0.01f)
                    catController.transform.rotation = Quaternion.Slerp(
                        catController.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 8f); // smoothly rotate to face direction of travel

                if (animator) animator.SetFloat("Speed", walkSpeed); // play walk animation

                yield return null;
            }
        }

        if (rb) rb.linearVelocity = Vector3.zero;  // stop the cat
        if (animator) animator.SetFloat("Speed", 0f); // stop walk animation
    }

    IEnumerator ZoomOut()
    {
        float originalDistance = cameraFollow.distance;
        float originalHeight = cameraFollow.height;

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration);
            cameraFollow.distance = Mathf.Lerp(originalDistance, targetDistance, t); // pull camera back
            cameraFollow.height = Mathf.Lerp(originalHeight, targetHeight, t);       // raise camera up
            yield return null;
        }

        cameraFollow.distance = targetDistance;
        cameraFollow.height = targetHeight;
    }

    IEnumerator FadeInMusic()
    {
        if (ambientMusicSource == null || ambientMusicClip == null) yield break;
        ambientMusicSource.clip = ambientMusicClip;
        ambientMusicSource.volume = 0f;
        ambientMusicSource.Play();
        float elapsed = 0f;
        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            ambientMusicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / musicFadeInDuration); // fade in
            yield return null;
        }
        ambientMusicSource.volume = musicVolume;
    }

    IEnumerator FadeOutAll()
    {
        float elapsed = 0f;
        float startVol = AudioListener.volume;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            if (fadeCanvas) fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t); // fade screen to black
            AudioListener.volume = Mathf.Lerp(startVol, 0f, t);       // fade all audio out
            yield return null;
        }

        if (fadeCanvas) fadeCanvas.alpha = 1f;
        AudioListener.volume = 0f;
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration); // fade from black
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }

    IEnumerator FadeInWind()
    {
        float elapsed = 0f;
        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.deltaTime;
            AudioListener.volume = Mathf.Lerp(0f, 1f, elapsed / audioFadeDuration); // restore global volume
            windAudio.volume = Mathf.Lerp(0f, 1f, elapsed / audioFadeDuration);     // raise wind volume
            yield return null;
        }
        AudioListener.volume = 1f;
        windAudio.volume = 1f;
    }
}