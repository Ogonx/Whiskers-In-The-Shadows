using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    enum State { Main, Options, Audio, Controls }
    State state = State.Main;

    [Header("Pause Root")]
    public GameObject pauseCanvasRoot; // the whole pause menu UI, shown/hidden on pause

    [Header("Panels")]
    public GameObject mainChoicesPanel;
    public GameObject optionsPanel;
    public GameObject audioPanel;
    public GameObject controlsPanel;

    [Header("Main Menu Items")]
    public TextMeshProUGUI[] mainGlowItems;

    [Header("Options Menu Items")]
    public TextMeshProUGUI[] optionsGlowItems;

    [Header("Audio UI")]
    public TextMeshProUGUI masterGlow;
    public TextMeshProUGUI audioBackGlow;
    public Slider volumeSlider;

    [Header("Glow")]
    public Color normalGlow = new Color(1, 1, 1, .25f);
    public Color selectedGlow = new Color(1, 1, 1, .75f);
    public float pulseAmount = .12f;
    public float pulseSpeed = 1.5f;

    [Header("Volume")]
    public float volumeStep = 0.05f;
    const string VolumePrefKey = "MASTER_VOLUME";

    [Header("Quit")]
    public string mainMenuSceneName = "MainMenu";

    [Header("UI Audio")]
    public AudioSource sfxSource;
    public AudioClip moveClip;
    public AudioClip confirmClip;

    [Header("Pause Audio")]
    public bool pauseAllSceneAudio = true; // whether to pause all audio sources on pause
    public bool pauseListener = false;     // whether to pause the AudioListener
    public bool muteListener = false;      // whether to mute the AudioListener instead

    int mainIndex;
    int optionsIndex;
    int audioIndex;
    bool audioEditing;
    bool paused;

    readonly List<AudioSource> pausedSources = new List<AudioSource>(); // tracks which sources were paused so they can be resumed
    float savedListenerVolume = 1f;

    void Start()
    {
        if (pauseCanvasRoot != null)
            pauseCanvasRoot.SetActive(false); // hide pause menu at start

        paused = false;
        Time.timeScale = 1f;

        LoadVolumeToSlider();
        SetState(State.Main);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (WasPressed(kb.escapeKey))
        {
            PlayMove();

            if (!paused) { Pause(); return; } // ESC pauses if not already paused

            // ESC navigates back through menu states when paused
            if (state == State.Audio && audioEditing) { audioEditing = false; ApplyAudioVisuals(); return; }
            if (state == State.Audio) { SetState(State.Options); return; }
            if (state == State.Options) { SetState(State.Main); return; }
            if (state == State.Controls) { SetState(State.Options); return; }

            Resume(); // ESC on main pause panel resumes game
            return;
        }

        if (!paused) return; // ignore all other input if not paused

        if (state == State.Controls)
        {
            if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey) || WasPressed(kb.backspaceKey))
                SetState(State.Options);
            return;
        }

        if (state == State.Audio)
        {
            if (audioEditing && audioIndex == 0)
            {
                if (WasPressed(kb.leftArrowKey) || WasPressed(kb.aKey)) { SetVolume(volumeSlider.value - volumeStep); PlayMove(); }
                if (WasPressed(kb.rightArrowKey) || WasPressed(kb.dKey)) { SetVolume(volumeSlider.value + volumeStep); PlayMove(); }

                if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey)) { audioEditing = false; PlayConfirm(); }

                PulseAudio();
                ApplyAudioVisuals();
                return;
            }

            if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey) || WasPressed(kb.downArrowKey) || WasPressed(kb.sKey))
            {
                audioIndex = 1 - audioIndex; // toggle between volume and back
                PlayMove();
                ApplyAudioVisuals();
            }

            if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey))
            {
                PlayConfirm();
                if (audioIndex == 0) audioEditing = true;
                else { SetState(State.Options); }
                ApplyAudioVisuals();
            }

            PulseAudio();
            return;
        }

        if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey)) { Step(-1); PlayMove(); ApplyVisuals(); }
        else if (WasPressed(kb.downArrowKey) || WasPressed(kb.sKey)) { Step(1); PlayMove(); ApplyVisuals(); }

        PulseSelected();

        if (WasPressed(kb.enterKey) || WasPressed(kb.spaceKey)) { PlayConfirm(); Activate(); }
    }

    void Pause()
    {
        paused = true;
        Time.timeScale = 0f; // freeze the game

        if (pauseCanvasRoot != null) pauseCanvasRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; // show cursor in pause menu

        PauseSceneAudio();
        SetState(State.Main);
    }

    void Resume()
    {
        paused = false;
        Time.timeScale = 1f; // unfreeze the game

        if (pauseCanvasRoot != null) pauseCanvasRoot.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; // hide cursor in gameplay

        ResumeSceneAudio();
    }

    void PauseSceneAudio()
    {
        pausedSources.Clear();

        if (pauseListener) AudioListener.pause = true;

        if (muteListener)
        {
            savedListenerVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }

        if (!pauseAllSceneAudio) return;

        foreach (var src in FindObjectsOfType<AudioSource>(true))
        {
            if (src == null || src == sfxSource) continue; // dont pause the menu sfx source
            if (!src.isPlaying) continue;

            src.Pause();
            pausedSources.Add(src); // remember it so it can be unpaused on resume
        }
    }

    void ResumeSceneAudio()
    {
        if (pauseListener) AudioListener.pause = false;
        if (muteListener) AudioListener.volume = savedListenerVolume;

        foreach (var src in pausedSources)
            if (src != null) src.UnPause(); // resume all paused sources

        pausedSources.Clear();
    }

    void Activate()
    {
        if (state == State.Main)
        {
            if (mainIndex == 0) { Resume(); return; }         // Resume
            if (mainIndex == 1) { SetState(State.Options); return; } // Options
            if (mainIndex == 2)
            {
                paused = false;
                Time.timeScale = 1f;
                ResumeSceneAudio();
                SceneManager.LoadScene(mainMenuSceneName); // Quit to Main Menu
                return;
            }
        }

        if (state == State.Options)
        {
            if (optionsIndex == 0) { SetState(State.Controls); return; }
            if (optionsIndex == 1) { SetState(State.Audio); return; }
            if (optionsIndex == 2) { SetState(State.Main); return; }
        }
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
        audioEditing = false;

        if (state == State.Main)
            mainIndex = Mathf.Clamp(mainIndex, 0, (mainGlowItems?.Length ?? 1) - 1);

        if (state == State.Options) optionsIndex = 0;
        if (state == State.Audio) audioIndex = 0;

        if (mainChoicesPanel != null) mainChoicesPanel.SetActive(state == State.Main);
        if (optionsPanel != null) optionsPanel.SetActive(state == State.Options);
        if (audioPanel != null) audioPanel.SetActive(state == State.Audio);
        if (controlsPanel != null) controlsPanel.SetActive(state == State.Controls);

        ApplyVisuals();
        ApplyAudioVisuals();

        if (state == State.Audio) LoadVolumeToSlider();
    }

    void LoadVolumeToSlider()
    {
        if (volumeSlider == null) return;
        volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefKey, 1f))); // load saved volume
    }

    void SetVolume(float v)
    {
        if (volumeSlider == null) return;
        v = Mathf.Clamp01(v);
        volumeSlider.SetValueWithoutNotify(v);
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumePrefKey, v);
        PlayerPrefs.Save();
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
        masterGlow.color = (audioIndex == 0) ? selectedGlow : normalGlow;
        audioBackGlow.color = (audioIndex == 1) ? selectedGlow : normalGlow;
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
    void PlayMove() { if (sfxSource != null && moveClip != null) sfxSource.PlayOneShot(moveClip, 0.7f); }
    void PlayConfirm() { if (sfxSource != null && confirmClip != null) sfxSource.PlayOneShot(confirmClip, 0.7f); }
}