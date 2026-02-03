using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    private enum State { Main, Options, Audio }
    private State state = State.Main;

    [Header("Panels")]
    public GameObject mainChoicesPanel;
    public GameObject optionsPanel;
    public GameObject audioPanel;

    [Header("Glow Items")]
    public TextMeshProUGUI[] mainGlowItems;     // 0 New Game, 1 Options
    public TextMeshProUGUI[] optionsGlowItems;  // 0 Controls, 1 Audio, 2 Back

    [Header("Audio Menu")]
    public TextMeshProUGUI masterGlow;    // MASTER_Glow inside AudioPanel
    public TextMeshProUGUI audioBackGlow; // BACK_Glow inside AudioPanel
    public Slider volumeSlider;           // UI only
    public float sliderStep = 0.05f;

    [Header("Glow Look")]
    public Color normalGlow = new Color(1f, 1f, 1f, 0.25f);
    public Color selectedGlow = new Color(1f, 1f, 1f, 0.75f);
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 1.5f;

    [Header("Scene")]
    public string gameplaySceneName = "SampleScene";

    [Header("Menu SFX")]
    public AudioSource sfxSource;
    public AudioClip moveClip;       // up/down + backspace exit edit
    public AudioClip confirmClip;    // enter on menu items / enter edit mode
    public AudioClip startGameClip;  // ONLY when starting New Game
    [Range(0f, 1f)] public float moveVol = 0.25f;
    [Range(0f, 1f)] public float confirmVol = 0.45f;
    [Range(0f, 1f)] public float startVol = 0.60f;

    private int mainIndex = 0;
    private int optionsIndex = 0;

    // Audio submenu
    private int audioIndex = 0;        // 0 = Master Volume, 1 = Back
    private bool audioEditing = false; // true when editing slider

    void Start()
    {
        SetState(State.Main);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // ESC always backs out one level (silent or you can add a backClip later)
        if (WasPressed(kb.escapeKey))
        {
            if (state == State.Audio)
            {
                audioEditing = false;
                SetState(State.Options);
                return;
            }
            if (state == State.Options)
            {
                SetState(State.Main);
                return;
            }
        }

        // -------- AUDIO MENU --------
        if (state == State.Audio)
        {
            // Backspace exits slider edit back to Master Volume selection
            if (audioEditing && WasPressed(kb.backspaceKey))
            {
                PlayMove();               // ✅ play move sound on backspace
                audioEditing = false;
                ApplyAudioVisuals();
                return;
            }

            // If editing slider: left/right adjusts (play move sound on each step)
            if (audioEditing)
            {
                if (volumeSlider != null)
                {
                    if (WasPressed(kb.leftArrowKey) || WasPressed(kb.aKey))
                    {
                        volumeSlider.value = Mathf.Clamp01(volumeSlider.value - sliderStep);
                        PlayMove();
                    }
                    else if (WasPressed(kb.rightArrowKey) || WasPressed(kb.dKey))
                    {
                        volumeSlider.value = Mathf.Clamp01(volumeSlider.value + sliderStep);
                        PlayMove();
                    }
                }

                // Enter while editing could optionally do nothing, or exit edit.
                // We'll leave it as "do nothing" for now.
                return;
            }

            // Navigate Audio menu (UP/DOWN)
            if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey))
            {
                audioIndex = 1 - audioIndex;
                PlayMove();
                ApplyAudioVisuals();
            }
            else if (WasPressed(kb.downArrowKey) || WasPressed(kb.sKey))
            {
                audioIndex = 1 - audioIndex;
                PlayMove();
                ApplyAudioVisuals();
            }

            // Enter on Audio menu item
            if (WasPressed(kb.enterKey) || WasPressed(kb.numpadEnterKey) || WasPressed(kb.spaceKey))
            {
                PlayConfirm(); // ✅ play confirm sound on enter in Audio menu

                if (audioIndex == 0)
                {
                    audioEditing = true; // enter slider edit
                }
                else
                {
                    SetState(State.Options); // BACK
                }

                ApplyAudioVisuals();
            }

            PulseAudioSelection();
            return;
        }

        // -------- MAIN / OPTIONS --------

        // Move selection (Up/Down) + MOVE SOUND
        if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey))
        {
            Step(-1);
            PlayMove();
            ApplyVisuals();
        }
        else if (WasPressed(kb.downArrowKey) || WasPressed(kb.sKey))
        {
            Step(+1);
            PlayMove();
            ApplyVisuals();
        }

        PulseSelected();

        // Enter select
        if (WasPressed(kb.enterKey) || WasPressed(kb.numpadEnterKey) || WasPressed(kb.spaceKey))
        {
            Activate();
        }
    }

    void Step(int dir)
    {
        if (state == State.Main)
        {
            if (mainGlowItems == null || mainGlowItems.Length == 0) return;
            mainIndex = (mainIndex + dir + mainGlowItems.Length) % mainGlowItems.Length;
        }
        else if (state == State.Options)
        {
            if (optionsGlowItems == null || optionsGlowItems.Length == 0) return;
            optionsIndex = (optionsIndex + dir + optionsGlowItems.Length) % optionsGlowItems.Length;
        }
    }

    void Activate()
    {
        if (state == State.Main)
        {
            // 0 = New Game, 1 = Options
            if (mainIndex == 0)
            {
                // ONLY play start sound when starting New Game
                PlayStartGame();
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                // Options stays silent (as you requested)
                SetState(State.Options);
            }
        }
        else if (state == State.Options)
        {
            // 0 Controls, 1 Audio, 2 Back
            if (optionsIndex == 1)
            {
                // silent enter to Audio (if you want confirm here, call PlayConfirm())
                audioIndex = 0;
                audioEditing = false;
                SetState(State.Audio);
            }
            else if (optionsIndex == 2)
            {
                // silent back
                SetState(State.Main);
            }
        }
    }

    void SetState(State s)
    {
        state = s;

        if (mainChoicesPanel != null) mainChoicesPanel.SetActive(state == State.Main);
        if (optionsPanel != null) optionsPanel.SetActive(state == State.Options);
        if (audioPanel != null) audioPanel.SetActive(state == State.Audio);

        if (state == State.Audio && audioPanel != null)
            audioPanel.transform.SetAsLastSibling(); // ensure on top

        ApplyVisuals();
        ApplyAudioVisuals();
    }

    void ApplyVisuals()
    {
        if (mainGlowItems != null)
        {
            for (int i = 0; i < mainGlowItems.Length; i++)
                if (mainGlowItems[i] != null)
                    mainGlowItems[i].color = (state == State.Main && i == mainIndex) ? selectedGlow : normalGlow;
        }

        if (optionsGlowItems != null)
        {
            for (int i = 0; i < optionsGlowItems.Length; i++)
                if (optionsGlowItems[i] != null)
                    optionsGlowItems[i].color = (state == State.Options && i == optionsIndex) ? selectedGlow : normalGlow;
        }
    }

    void ApplyAudioVisuals()
    {
        if (masterGlow == null || audioBackGlow == null) return;

        if (!audioEditing)
        {
            masterGlow.color = (audioIndex == 0) ? selectedGlow : normalGlow;
            audioBackGlow.color = (audioIndex == 1) ? selectedGlow : normalGlow;
        }
        else
        {
            // while editing, keep both dim (you can change this if you want)
            masterGlow.color = normalGlow;
            audioBackGlow.color = normalGlow;
        }
    }

    void PulseSelected()
    {
        TextMeshProUGUI t = null;
        if (state == State.Main && mainGlowItems != null && mainGlowItems.Length > 0) t = mainGlowItems[mainIndex];
        else if (state == State.Options && optionsGlowItems != null && optionsGlowItems.Length > 0) t = optionsGlowItems[optionsIndex];
        if (t == null) return;

        float p = 1f - pulseAmount + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;
        Color c = selectedGlow; c.r *= p; c.g *= p; c.b *= p;
        t.color = c;
    }

    void PulseAudioSelection()
    {
        if (audioEditing) return;

        TextMeshProUGUI t = (audioIndex == 0) ? masterGlow : audioBackGlow;
        if (t == null) return;

        float p = 1f - pulseAmount + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;
        Color c = selectedGlow; c.r *= p; c.g *= p; c.b *= p;
        t.color = c;
    }

    bool WasPressed(KeyControl key) => key != null && key.wasPressedThisFrame;

    void PlayMove()
    {
        if (sfxSource != null && moveClip != null)
            sfxSource.PlayOneShot(moveClip, moveVol);
    }

    void PlayConfirm()
    {
        if (sfxSource != null && confirmClip != null)
            sfxSource.PlayOneShot(confirmClip, confirmVol);
    }

    void PlayStartGame()
    {
        if (sfxSource != null && startGameClip != null)
            sfxSource.PlayOneShot(startGameClip, startVol);
    }
}
