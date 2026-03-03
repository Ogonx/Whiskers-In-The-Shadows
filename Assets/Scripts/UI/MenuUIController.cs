using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    enum State { Main, Options, Audio }
    State state = State.Main;

    [Header("Panels")]
    public GameObject mainChoicesPanel;
    public GameObject optionsPanel;
    public GameObject audioPanel;

    [Header("Glow Items")]
    public TextMeshProUGUI[] mainGlowItems;
    public TextMeshProUGUI[] optionsGlowItems;

    [Header("Audio UI")]
    public TextMeshProUGUI masterGlow;
    public TextMeshProUGUI audioBackGlow;
    public Slider volumeSlider;

    [Header("Volume")]
    [Range(0f, 1f)] public float defaultVolume = 1f;
    public float volumeStep = 0.05f;
    public float holdRepeatDelay = 0.28f;
    public float holdRepeatRate = 0.06f;
    const string VolumePrefKey = "MASTER_VOLUME";

    [Header("Glow")]
    public Color normalGlow = new Color(1, 1, 1, .25f);
    public Color selectedGlow = new Color(1, 1, 1, .75f);
    public float pulseAmount = .12f;
    public float pulseSpeed = 1.5f;

    [Header("Scene")]
    public string gameplaySceneName = "MainScene";

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip moveClip;
    public AudioClip confirmClip;
    public AudioClip startGameClip;

    [Header("Fade")]
    public CanvasGroup fadeOverlay2;
    public CanvasGroup blackOverlay;
    public float fadeInOnStart = 1.2f;
    public float fadeToBlackOnNewGame = 0.8f;

    [Header("Paw Stamps")]
    public PawStamp pawStamp;
    public float afterPawsDelay = 0.15f;

    int mainIndex;
    int optionsIndex;
    int audioIndex;
    bool audioEditing;
    bool loading;

    float holdTimer;
    float holdRepeatTimer;
    int holdDir;

    void Start()
    {
        SetState(State.Main);
        SetupMasterVolume();

        if (fadeOverlay2 != null)
        {
            fadeOverlay2.gameObject.SetActive(true);
            fadeOverlay2.alpha = 1f;
            fadeOverlay2.blocksRaycasts = true;
            fadeOverlay2.interactable = false;
            fadeOverlay2.transform.SetAsLastSibling();
            StartCoroutine(FadeCanvasGroup(fadeOverlay2, 1f, 0f, fadeInOnStart));
        }

        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(true);
            blackOverlay.alpha = 0f;
            blackOverlay.blocksRaycasts = true;
            blackOverlay.interactable = false;
        }

        if (pawStamp != null) pawStamp.gameObject.SetActive(false);
    }

    void SetupMasterVolume()
    {
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefKey, defaultVolume));
        ApplyVolume(saved);

        if (volumeSlider == null) return;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;
        volumeSlider.SetValueWithoutNotify(saved);
        volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
        volumeSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float v) { ApplyVolume(v); SaveVolume(v); }

    void ApplyVolume(float v) => AudioListener.volume = Mathf.Clamp01(v);

    void SaveVolume(float v)
    {
        PlayerPrefs.SetFloat(VolumePrefKey, Mathf.Clamp01(v));
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (loading) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (WasPressed(kb.escapeKey))
        {
            if (state == State.Audio && audioEditing) { audioEditing = false; holdDir = 0; PlayConfirm(); ApplyAudioVisuals(); return; }
            if (state == State.Audio) { SetState(State.Options); return; }
            if (state == State.Options) { SetState(State.Main); return; }
        }

        if (state == State.Audio)
        {
            if (audioEditing && audioIndex == 0)
            {
                HandleVolumeAdjust(kb);

                if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey))
                {
                    audioEditing = false;
                    holdDir = 0;
                    PlayConfirm();
                    ApplyAudioVisuals();
                }

                PulseAudio();
                return;
            }

            if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey) || WasPressed(kb.downArrowKey) || WasPressed(kb.sKey))
            {
                audioIndex = 1 - audioIndex;
                PlayMove();
                ApplyAudioVisuals();
            }

            if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey))
            {
                PlayConfirm();
                if (audioIndex == 0) { audioEditing = true; holdDir = 0; holdTimer = 0f; holdRepeatTimer = 0f; ApplyAudioVisuals(); }
                else SetState(State.Options);
            }

            PulseAudio();
            return;
        }

        if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey)) { Step(-1); PlayMove(); ApplyVisuals(); }
        else if (WasPressed(kb.downArrowKey) || WasPressed(kb.sKey)) { Step(1); PlayMove(); ApplyVisuals(); }

        PulseSelected();

        if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey)) Activate();
    }

    void HandleVolumeAdjust(Keyboard kb)
    {
        if (volumeSlider == null) return;

        bool leftDown = kb.leftArrowKey.isPressed || kb.aKey.isPressed;
        bool rightDown = kb.rightArrowKey.isPressed || kb.dKey.isPressed;

        if (WasPressed(kb.leftArrowKey) || WasPressed(kb.aKey)) { NudgeVolume(-1); holdDir = -1; holdTimer = holdRepeatTimer = 0f; return; }
        if (WasPressed(kb.rightArrowKey) || WasPressed(kb.dKey)) { NudgeVolume(+1); holdDir = +1; holdTimer = holdRepeatTimer = 0f; return; }

        if ((holdDir == -1 && leftDown) || (holdDir == +1 && rightDown))
        {
            holdTimer += Time.unscaledDeltaTime;
            if (holdTimer >= holdRepeatDelay)
            {
                holdRepeatTimer += Time.unscaledDeltaTime;
                while (holdRepeatTimer >= holdRepeatRate) { holdRepeatTimer -= holdRepeatRate; NudgeVolume(holdDir); }
            }
        }
        else
        {
            holdDir = 0;
            holdTimer = holdRepeatTimer = 0f;
        }
    }

    void NudgeVolume(int dir)
    {
        if (volumeSlider == null) return;

        float v = Mathf.Clamp01(volumeSlider.value + dir * volumeStep);
        volumeSlider.SetValueWithoutNotify(v);
        ApplyVolume(v);
        SaveVolume(v);
        PlayMove();
    }

    void Activate()
    {
        if (state == State.Main)
        {
            if (mainIndex == 0) StartCoroutine(NewGameFlow());
            else { PlayConfirm(); SetState(State.Options); }
        }
        else if (state == State.Options)
        {
            PlayConfirm();
            if (optionsIndex == 1) SetState(State.Audio);
            else if (optionsIndex == 2) SetState(State.Main);
        }
    }

    IEnumerator NewGameFlow()
    {
        loading = true;
        PlayStartGame();

        if (fadeOverlay2 != null)
        {
            fadeOverlay2.gameObject.SetActive(true);
            fadeOverlay2.transform.SetAsLastSibling();
            yield return FadeCanvasGroup(fadeOverlay2, fadeOverlay2.alpha, 1f, fadeToBlackOnNewGame);
            fadeOverlay2.alpha = 1f;
        }

        if (blackOverlay != null) { blackOverlay.alpha = 1f; blackOverlay.transform.SetAsLastSibling(); }
        if (fadeOverlay2 != null) fadeOverlay2.gameObject.SetActive(false);
        if (pawStamp != null) yield return pawStamp.Play();
        if (afterPawsDelay > 0f) yield return new WaitForSecondsRealtime(afterPawsDelay);

        SceneManager.LoadScene(gameplaySceneName);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        if (cg == null) yield break;
        if (dur <= 0.0001f) { cg.alpha = to; yield break; }

        float t = 0f;
        cg.alpha = from;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }

        cg.alpha = to;
        if (cg == fadeOverlay2) cg.blocksRaycasts = false;
    }

    void Step(int d)
    {
        if (state == State.Main && mainGlowItems != null && mainGlowItems.Length > 0)
            mainIndex = (mainIndex + d + mainGlowItems.Length) % mainGlowItems.Length;

        if (state == State.Options && optionsGlowItems != null && optionsGlowItems.Length > 0)
            optionsIndex = (optionsIndex + d + optionsGlowItems.Length) % optionsGlowItems.Length;
    }

    void SetState(State s)
    {
        state = s;

        if (mainChoicesPanel != null) mainChoicesPanel.SetActive(state == State.Main);
        if (optionsPanel != null) optionsPanel.SetActive(state == State.Options);
        if (audioPanel != null) audioPanel.SetActive(state == State.Audio);

        if (state != State.Audio) { audioEditing = false; holdDir = 0; }

        ApplyVisuals();
        ApplyAudioVisuals();
    }

    void ApplyVisuals()
    {
        if (mainGlowItems != null)
            for (int i = 0; i < mainGlowItems.Length; i++)
                if (mainGlowItems[i] != null)
                    mainGlowItems[i].color = (state == State.Main && i == mainIndex) ? selectedGlow : normalGlow;

        if (optionsGlowItems != null)
            for (int i = 0; i < optionsGlowItems.Length; i++)
                if (optionsGlowItems[i] != null)
                    optionsGlowItems[i].color = (state == State.Options && i == optionsIndex) ? selectedGlow : normalGlow;
    }

    void ApplyAudioVisuals()
    {
        if (masterGlow == null || audioBackGlow == null) return;

        if (state != State.Audio) { masterGlow.color = normalGlow; audioBackGlow.color = normalGlow; return; }

        masterGlow.color = (audioEditing && audioIndex == 0) || audioIndex == 0 ? selectedGlow : normalGlow;
        audioBackGlow.color = audioIndex == 1 ? selectedGlow : normalGlow;
    }

    void PulseSelected()
    {
        TextMeshProUGUI t = null;

        if (state == State.Main && mainGlowItems != null && mainGlowItems.Length > 0) t = mainGlowItems[mainIndex];
        else if (state == State.Options && optionsGlowItems != null && optionsGlowItems.Length > 0) t = optionsGlowItems[optionsIndex];

        if (t == null) return;

        float p = 1 - pulseAmount + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * .5f + .5f) * pulseAmount;
        t.color = new Color(selectedGlow.r * p, selectedGlow.g * p, selectedGlow.b * p, selectedGlow.a);
    }

    void PulseAudio()
    {
        if (state != State.Audio) return;

        var t = audioIndex == 0 ? masterGlow : audioBackGlow;
        if (t == null) return;

        float p = 1 - pulseAmount + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * .5f + .5f) * pulseAmount;
        t.color = new Color(selectedGlow.r * p, selectedGlow.g * p, selectedGlow.b * p, selectedGlow.a);
    }

    bool WasPressed(KeyControl k) => k != null && k.wasPressedThisFrame;
    void PlayMove() { if (sfxSource && moveClip) sfxSource.PlayOneShot(moveClip, 0.7f); }
    void PlayConfirm() { if (sfxSource && confirmClip) sfxSource.PlayOneShot(confirmClip, 0.7f); }
    void PlayStartGame() { if (sfxSource && startGameClip) sfxSource.PlayOneShot(startGameClip, 0.8f); }
}