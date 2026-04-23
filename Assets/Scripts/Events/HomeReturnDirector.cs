using UnityEngine;
using System.Collections;

public class HomeReturnDirector : MonoBehaviour
{
    [Header("References")]
    public CatController catController;
    public PSXCameraFollow cameraFollow;
    public Camera mainCamera;

    [Header("Cat Path")]
    public Transform[] waypoints;
    public float walkSpeed = 2f;

    [Header("Teleport")]
    public Transform homeSpawnPoint;

    [Header("Audio")]
    public AudioSource windAudio;
    public float audioFadeDuration = 2f;

    [Header("Music")]
    public AudioSource ambientMusicSource;
    public AudioClip ambientMusicClip;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    public float musicFadeInDuration = 2f;

    [Header("Camera")]
    public float targetDistance = 12f;
    public float targetHeight = 8f;
    public float riseDuration = 4f;

    [Header("Fade")]
    public float fadeOutDuration = 2f;
    public float fadeInDuration = 2f;

    [Header("Trail")]
    public ScentTrail houseTrail;

    [Header("Ending")]
    public SleepEndingDirector sleepEndingDirector;

    bool triggered = false;
    CanvasGroup fadeCanvas;

    void Start()
    {
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
        fadeCanvas.alpha = 0f;
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
        catController.FreezeMovement();

        cameraFollow.frozen = false;
        cameraFollow.blendingBack = false;
        cameraFollow.frontMode = false;

        if (windAudio) windAudio.volume = 0f;
        if (windAudio) windAudio.Stop();

        StartCoroutine(ZoomOut());
        StartCoroutine(FadeInMusic());

        yield return StartCoroutine(WalkCatAlongPath());

        yield return StartCoroutine(FadeOutAll());

        catController.transform.position = homeSpawnPoint.position;
        catController.transform.rotation = homeSpawnPoint.rotation;

        cameraFollow.distance = 4f;
        cameraFollow.height = 2f;

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeIn());

        if (windAudio)
        {
            windAudio.volume = 0f;
            windAudio.Play();
            yield return StartCoroutine(FadeInWind());
        }

        if (houseTrail) houseTrail.UnlockAndShow();

        if (sleepEndingDirector) sleepEndingDirector.Unlock();

        catController.UnfreezeMovement();

        gameObject.SetActive(false);
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
                    rb.linearVelocity = vel;
                }

                if (dir.sqrMagnitude > 0.01f)
                    catController.transform.rotation = Quaternion.Slerp(
                        catController.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 8f);

                if (animator) animator.SetFloat("Speed", walkSpeed);

                yield return null;
            }
        }

        if (rb) rb.linearVelocity = Vector3.zero;
        if (animator) animator.SetFloat("Speed", 0f);
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
            cameraFollow.distance = Mathf.Lerp(originalDistance, targetDistance, t);
            cameraFollow.height = Mathf.Lerp(originalHeight, targetHeight, t);
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
            ambientMusicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / musicFadeInDuration);
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
            if (fadeCanvas) fadeCanvas.alpha = Mathf.Lerp(0f, 1f, t);
            AudioListener.volume = Mathf.Lerp(startVol, 0f, t);
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
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
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
            AudioListener.volume = Mathf.Lerp(0f, 1f, elapsed / audioFadeDuration);
            windAudio.volume = Mathf.Lerp(0f, 1f, elapsed / audioFadeDuration);
            yield return null;
        }
        AudioListener.volume = 1f;
        windAudio.volume = 1f;
    }
}