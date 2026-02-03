using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public class MenuSelector_NewInput : MonoBehaviour
{
    [Header("Menu Glow Texts ONLY (Top -> Bottom)")]
    [Tooltip("Drag ONLY the Glow TMP objects here (e.g., NEWGAME_Glow, OPTIONS_Glow). Do NOT drag the Fill texts.")]
    public TextMeshProUGUI[] glowItems;

    [Header("Glow Look")]
    [Tooltip("Faint glow (unselected). White with low alpha.")]
    public Color normalGlow = new Color(1f, 1f, 1f, 0.25f);

    [Tooltip("Stronger glow (selected). White with higher alpha.")]
    public Color selectedGlow = new Color(1f, 1f, 1f, 0.75f);

    [Tooltip("How much the selected glow pulses. Keep subtle for Silent Hill vibe.")]
    public float pulseAmount = 0.12f;

    [Tooltip("Pulse speed (slow/subtle).")]
    public float pulseSpeed = 1.5f;

    [Header("Actions")]
    public string gameplaySceneName = "SampleScene"; // change to your gameplay scene

    [Header("SFX (optional)")]
    public AudioSource sfxSource;
    public AudioClip moveClip;
    public AudioClip selectClip;

    private int index = 0;

    void Start()
    {
        ApplyVisuals();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (glowItems == null || glowItems.Length == 0) return;

        // Move selection (Up / W)
        if (WasPressed(kb.upArrowKey) || WasPressed(kb.wKey))
        {
            index = (index - 1 + glowItems.Length) % glowItems.Length;
            Play(moveClip);
            ApplyVisuals();
        }
        // Move selection (Down / S)
        else if (WasPressed(kb.downArrowKey) || WasPressed(kb.sKey))
        {
            index = (index + 1) % glowItems.Length;
            Play(moveClip);
            ApplyVisuals();
        }

        PulseSelected();

        // Select (Enter / Space)
        if (WasPressed(kb.enterKey) || WasPressed(kb.numpadEnterKey) || WasPressed(kb.spaceKey))
        {
            Play(selectClip);
            ActivateSelection();
        }
    }

    // Works like GetKeyDown
    bool WasPressed(KeyControl key) => key != null && key.wasPressedThisFrame;

    void ApplyVisuals()
    {
        for (int i = 0; i < glowItems.Length; i++)
        {
            if (glowItems[i] == null) continue;
            glowItems[i].color = (i == index) ? selectedGlow : normalGlow;
        }
    }

    void PulseSelected()
    {
        var t = glowItems[index];
        if (t == null) return;

        // p goes between (1 - pulseAmount) and 1
        float p = 1f - pulseAmount + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;

        // Only pulse the glow intensity (RGB) while keeping alpha based on selectedGlow
        Color c = selectedGlow;
        c.r *= p;
        c.g *= p;
        c.b *= p;
        // keep alpha as-is (so it doesn't "blink")
        t.color = c;
    }

    void ActivateSelection()
    {
        // 0 = New Game, 1 = Options (based on order in glowItems)
        if (index == 0)
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else if (index == 1)
        {
            Debug.Log("Open Options Panel");
            // Later: show options UI panel here
        }
    }

    void Play(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }
}
