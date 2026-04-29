using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroCutsceneDirector : MonoBehaviour
{
    [Header("Eyelids")]
    [SerializeField] RectTransform topLid;
    [SerializeField] RectTransform bottomLid;
    [SerializeField] float openGapY = 18f;
    [SerializeField] float closedGapY = 1800f;
    [SerializeField] AnimationCurve lidCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Cat Camera")]
    [SerializeField] Transform catCamera; // the cat's POV camera used for the pan and wobble

    [Header("Head Wobble")]
    [SerializeField] float wobbleDuration = 0.6f;
    [SerializeField] float wobblePitch = 1.2f;
    [SerializeField] float wobbleRoll = 0.8f;
    [SerializeField] float wobbleSpeed = 6f;

    [Header("Blur Overlay")]
    [SerializeField] CanvasGroup blurOverlay;
    [Range(0f, 1f)][SerializeField] float blurStartAlpha = 0.55f;
    [SerializeField] float blurFadeTime = 1.2f;

    [Header("Audio Sources")]
    [SerializeField] AudioSource rainSource;
    [SerializeField] AudioSource clockSource;
    [SerializeField] AudioSource purrSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource footstepsSource;

    [Header("Audio Clips")]
    [SerializeField] AudioClip rainLoop;
    [SerializeField] AudioClip clockLoop;
    [SerializeField] AudioClip purrLoop;
    [SerializeField] AudioClip thunderClip;
    [SerializeField] AudioClip doorOpenCloseClip;
    [SerializeField] AudioClip footstepsLoop;
    [SerializeField] AudioClip petClip;

    [Header("Audio Fade In")]
    [SerializeField] float fadeInTimeRain = 2.0f;
    [SerializeField] float fadeInTimeClock = 1.2f;
    [SerializeField] float fadeInTimePurr = 0.8f;
    [Range(0f, 1f)][SerializeField] float rainTargetVolume = 1f;
    [Range(0f, 1f)][SerializeField] float clockTargetVolume = 0.65f;

    [Header("Purr")]
    [Range(0f, 1f)][SerializeField] float purrSleepVolume = 0.55f;  // purr volume when eyes closed
    [Range(0f, 1f)][SerializeField] float purrAwakeVolume = 0.20f;  // purr volume when eyes open
    [SerializeField] float purrDuckFadeTime = 0.35f;

    [Header("Door Beat")]
    [SerializeField] float blackHoldBeforeAnything = 1.5f;     // hold black at start before anything plays
    [SerializeField] float thunderDelay = 0.6f;
    [SerializeField] float doorDelayAfterThunder = 0.4f;
    [SerializeField] float openOnDoorTime = 3.5f;              // how long the eyes take to open on door sound
    [SerializeField] float doorCloseStartsAt = 3.0f;           // when during the door beat the eyes start closing
    [SerializeField] float closeAfterDoorCloseTime = 1.3f;
    [SerializeField] float closedHoldAfterDoorBeat = 0.6f;

    [Header("Person")]
    [SerializeField] Transform person;       // the owner character
    [SerializeField] Transform walkStart;    // where the owner starts
    [SerializeField] Transform stopPoint1;   // first pause point
    [SerializeField] Transform stopPoint2;   // second pause point where petting happens
    [SerializeField] Transform exitPoint;    // where the owner walks to and disappears
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float rotateSpeed = 540f;
    [SerializeField] float arriveDistance = 0.05f;
    [Range(0f, 1f)][SerializeField] float footstepsVolume = 1f;
    [SerializeField] float stopLookHold = 0.9f; // how long the owner looks at the cat before walking on

    [Header("Animator")]
    [SerializeField] Animator personAnimator;
    [SerializeField] string walkingBoolName = "IsWalking";

    [Header("Blink")]
    [SerializeField] float blinkCloseTime_Stop1 = 0.35f;
    [SerializeField] float blinkClosedHold_Stop1 = 0.12f;
    [SerializeField] float blinkOpenTime_Stop1 = 0.45f;

    [Header("Pet Moment")]
    [SerializeField] float panDownDegrees = 20f;               // how far the camera tilts down during petting
    [SerializeField] float panDownTime = 1.8f;
    [SerializeField] float eyesCloseAfterPetTime = 1.2f;
    [SerializeField] float petSoundDelayAfterClosed = 0.05f;
    [SerializeField] float eyesClosedHoldBeforeLookUp = 0.45f;
    [SerializeField] float eyesOpenToLookUpTime = 1.0f;
    [SerializeField] float panUpTime = 1.2f;
    [SerializeField] float watchExitHold = 0.15f;

    [Header("Sleep Fade")]
    [SerializeField] float fadeOutTime = 2.0f;
    [SerializeField] float finalCloseTime = 1.4f;
    [SerializeField] float finalBlackHold = 0.4f;

    [Header("Next Scene")]
    [SerializeField] string nextSceneName = "MainScene";

    Coroutine purrFadeRoutine;  // stored so new fades can cancel the previous one
    Coroutine blurFadeRoutine;

    void Start()
    {
        SetLids(closedGapY); // start with eyes closed

        if (blurOverlay)
        {
            blurOverlay.alpha = 0f;
            blurOverlay.blocksRaycasts = false;
            blurOverlay.interactable = false;
        }

        // start all audio loops silently
        StartLoopAtZero(rainSource, rainLoop);
        StartLoopAtZero(clockSource, clockLoop);
        StartLoopAtZero(purrSource, purrLoop);

        // fade each audio source in
        if (rainSource) StartCoroutine(FadeAudio(rainSource, 0f, rainTargetVolume, fadeInTimeRain));
        if (clockSource) StartCoroutine(FadeAudio(clockSource, 0f, clockTargetVolume, fadeInTimeClock));
        if (purrSource) StartCoroutine(FadeAudio(purrSource, 0f, purrSleepVolume, fadeInTimePurr));

        if (footstepsSource)
        {
            footstepsSource.Stop();
            footstepsSource.loop = true;
            footstepsSource.volume = footstepsVolume;
        }

        if (personAnimator) personAnimator.applyRootMotion = false;

        StartCoroutine(CutsceneRoutine());
    }

    IEnumerator CutsceneRoutine()
    {
        yield return new WaitForSecondsRealtime(blackHoldBeforeAnything);
        yield return new WaitForSecondsRealtime(thunderDelay);

        PlaySfx(thunderClip); // thunder sound

        yield return new WaitForSecondsRealtime(doorDelayAfterThunder);
        yield return DoorBeatRoutine(); // door opens, eyes open, eyes close
        yield return new WaitForSecondsRealtime(closedHoldAfterDoorBeat);

        if (person && walkStart) person.position = walkStart.position; // place owner at start

        yield return OpenEyes(2.6f); // open eyes as owner comes in

        if (person && stopPoint1)
            yield return WalkTo(stopPoint1, true, Camera.main ? Camera.main.transform : null);

        yield return Blink(blinkCloseTime_Stop1, blinkClosedHold_Stop1, blinkOpenTime_Stop1); // quick blink
        yield return new WaitForSecondsRealtime(stopLookHold);

        if (person && stopPoint2)
            yield return WalkTo(stopPoint2, true, Camera.main ? Camera.main.transform : null);

        yield return PetMomentRoutine(); // owner pets the cat, eyes close
        yield return CloseEyes(finalCloseTime);
        yield return FadeOutToSleep(); // fade all audio out
        yield return new WaitForSecondsRealtime(finalBlackHold);

        WakeState.PlayWakeSequenceOnLoad = true; // tell MainScene to play the wake sequence
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator PetMomentRoutine()
    {
        yield return PanCameraPitchOnly(panDownDegrees, panDownTime); // tilt camera down
        yield return CloseEyes(eyesCloseAfterPetTime);                // cat closes eyes being petted

        if (petSoundDelayAfterClosed > 0f)
            yield return new WaitForSecondsRealtime(petSoundDelayAfterClosed);

        PlaySfx(petClip); // play petting sound

        if (petClip != null)
            yield return new WaitForSecondsRealtime(petClip.length);

        yield return new WaitForSecondsRealtime(eyesClosedHoldBeforeLookUp);
        yield return OpenEyes(eyesOpenToLookUpTime);               // cat opens eyes
        yield return PanCameraPitchOnly(0f, panUpTime);            // tilt camera back up
        yield return new WaitForSecondsRealtime(watchExitHold);

        if (person && exitPoint)
            yield return WalkTo(exitPoint, false, null); // owner walks away and exits
    }

    IEnumerator DoorBeatRoutine()
    {
        if (sfxSource && doorOpenCloseClip) sfxSource.PlayOneShot(doorOpenCloseClip); // door sound

        yield return OpenEyes(openOnDoorTime); // eyes open on door sound

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, doorCloseStartsAt - openOnDoorTime));
        yield return CloseEyes(closeAfterDoorCloseTime); // eyes close again
    }

    IEnumerator OpenEyes(float dur)
    {
        SetCatAwake(true);  // lower purr volume
        StartBlurPulse();   // brief blur flash as eyes open
        yield return MoveLids(closedGapY, openGapY, dur);
        yield return MicroHeadWobble(); // small head movement after opening
    }

    IEnumerator CloseEyes(float dur)
    {
        ForceBlurOff();
        yield return MoveLids(openGapY, closedGapY, dur);
        SetCatAwake(false); // raise purr volume
    }

    IEnumerator Blink(float closeTime, float closedHold, float openTime)
    {
        yield return CloseEyes(closeTime);
        yield return new WaitForSecondsRealtime(closedHold);
        yield return OpenEyes(openTime);
    }

    void StartBlurPulse()
    {
        if (!blurOverlay) return;
        if (blurFadeRoutine != null) StopCoroutine(blurFadeRoutine);
        blurOverlay.alpha = blurStartAlpha;
        blurFadeRoutine = StartCoroutine(FadeCanvasGroup(blurOverlay, blurStartAlpha, 0f, blurFadeTime));
    }

    void ForceBlurOff()
    {
        if (!blurOverlay) return;
        if (blurFadeRoutine != null) StopCoroutine(blurFadeRoutine);
        blurFadeRoutine = null;
        blurOverlay.alpha = 0f;
    }

    void SetCatAwake(bool awake)
    {
        FadePurrTo(awake ? purrAwakeVolume : purrSleepVolume, purrDuckFadeTime); // change purr volume based on eye state
    }

    void FadePurrTo(float target, float dur)
    {
        if (!purrSource) return;
        if (purrFadeRoutine != null) StopCoroutine(purrFadeRoutine);
        purrFadeRoutine = StartCoroutine(FadeAudio(purrSource, purrSource.volume, target, dur));
    }

    IEnumerator MoveLids(float from, float to, float dur)
    {
        if (!topLid || !bottomLid) yield break;
        if (dur <= 0.0001f) { SetLids(to); yield break; }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float eased = lidCurve != null ? lidCurve.Evaluate(Mathf.Clamp01(t / dur)) : Mathf.Clamp01(t / dur);
            SetLids(Mathf.Lerp(from, to, eased)); // move lids toward target position
            yield return null;
        }

        SetLids(to);
    }

    void SetLids(float gapY)
    {
        SetAnchoredY(topLid, -gapY);  // top lid moves up
        SetAnchoredY(bottomLid, gapY); // bottom lid moves down
    }

    static void SetAnchoredY(RectTransform rt, float y)
    {
        if (!rt) return;
        var p = rt.anchoredPosition;
        p.y = y;
        rt.anchoredPosition = p;
    }

    IEnumerator PanCameraPitchOnly(float targetPitchDegrees, float dur)
    {
        if (!catCamera) yield break;

        Vector3 startEuler = catCamera.localEulerAngles;
        float startX = startEuler.x > 180f ? startEuler.x - 360f : startEuler.x; // convert to signed angle

        if (dur <= 0.0001f) { catCamera.localEulerAngles = new Vector3(targetPitchDegrees, startEuler.y, startEuler.z); yield break; }

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            catCamera.localEulerAngles = new Vector3(Mathf.Lerp(startX, targetPitchDegrees, Mathf.Clamp01(t / dur)), startEuler.y, startEuler.z);
            yield return null;
        }

        catCamera.localEulerAngles = new Vector3(targetPitchDegrees, startEuler.y, startEuler.z);
    }

    IEnumerator MicroHeadWobble()
    {
        if (!catCamera) yield break;

        Vector3 baseEuler = catCamera.localEulerAngles;
        float baseX = baseEuler.x > 180f ? baseEuler.x - 360f : baseEuler.x;
        float baseZ = baseEuler.z > 180f ? baseEuler.z - 360f : baseEuler.z;

        float t = 0f;
        while (t < wobbleDuration)
        {
            t += Time.deltaTime;
            float wob = Mathf.Sin(t * wobbleSpeed) * (1f - Mathf.Clamp01(t / wobbleDuration)); // wobble fades out over time
            catCamera.localEulerAngles = new Vector3(baseX + wob * wobblePitch, baseEuler.y, baseZ + wob * wobbleRoll);
            yield return null;
        }

        catCamera.localEulerAngles = new Vector3(baseX, baseEuler.y, baseZ); // snap back to base rotation
    }

    IEnumerator WalkTo(Transform targetPos, bool faceTargetWhenArrived, Transform faceTarget)
    {
        if (!person || !targetPos) yield break;

        SetWalking(true);

        while (Vector3.Distance(person.position, targetPos.position) > arriveDistance)
        {
            person.position = Vector3.MoveTowards(person.position, targetPos.position, walkSpeed * Time.deltaTime);

            Vector3 dir = targetPos.position - person.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                person.rotation = Quaternion.RotateTowards(person.rotation, Quaternion.LookRotation(dir.normalized), rotateSpeed * Time.deltaTime);

            yield return null;
        }

        SetWalking(false);

        if (faceTargetWhenArrived && faceTarget)
        {
            // rotate owner to face camera over 0.35 seconds
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                Vector3 dir = faceTarget.position - person.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    person.rotation = Quaternion.RotateTowards(person.rotation, Quaternion.LookRotation(dir.normalized), rotateSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    void SetWalking(bool walking)
    {
        if (personAnimator) personAnimator.SetBool(walkingBoolName, walking);
        if (!footstepsSource || !footstepsLoop) return;

        if (walking)
        {
            if (footstepsSource.isPlaying) return;
            footstepsSource.clip = footstepsLoop;
            footstepsSource.loop = true;
            footstepsSource.volume = footstepsVolume;
            footstepsSource.Play(); // start footsteps when walking
        }
        else
        {
            if (footstepsSource.isPlaying) footstepsSource.Stop(); // stop footsteps when standing still
        }
    }

    static void StartLoopAtZero(AudioSource src, AudioClip clip)
    {
        if (!src || !clip) return;
        src.clip = clip;
        src.loop = true;
        src.volume = 0f;       // start silent so it can be faded in
        if (!src.isPlaying) src.Play();
    }

    IEnumerator FadeAudio(AudioSource src, float from, float to, float dur)
    {
        if (!src) yield break;
        if (dur <= 0.0001f) { src.volume = to; yield break; }

        float t = 0f;
        src.volume = from;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur)); // gradually change volume
            yield return null;
        }

        src.volume = to;
    }

    IEnumerator FadeOutToSleep()
    {
        if (footstepsSource && footstepsSource.isPlaying) footstepsSource.Stop();

        // fade all three audio sources out at the same time
        Coroutine a = rainSource ? StartCoroutine(FadeAudio(rainSource, rainSource.volume, 0f, fadeOutTime)) : null;
        Coroutine b = clockSource ? StartCoroutine(FadeAudio(clockSource, clockSource.volume, 0f, fadeOutTime)) : null;
        Coroutine c = purrSource ? StartCoroutine(FadeAudio(purrSource, purrSource.volume, 0f, fadeOutTime)) : null;

        if (a != null) yield return a;
        if (b != null) yield return b;
        if (c != null) yield return c;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        if (!cg) yield break;
        if (dur <= 0.0001f) { cg.alpha = to; yield break; }

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

    void PlaySfx(AudioClip clip)
    {
        if (sfxSource && clip) sfxSource.PlayOneShot(clip);
    }
}