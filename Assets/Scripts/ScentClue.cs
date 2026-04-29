using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScentClue : MonoBehaviour
{
    public static ScentTrail CurrentActiveTrail; // static reference to the currently active trail so Q can re-show it from anywhere

    public bool IsUsed => used; // read-only property checked by JumpscareTriggerGate and ExitBlocker

    [Header("Interaction")]
    public float interactRange = 2f;
    public bool oneTimeOnly = true; // if true this clue can only be activated once

    [Header("What this clue unlocks")]
    public ScentTrail trailToUnlock;

    [Header("Particle")]
    public GameObject particleToHide; // particle effect on the clue object, hidden after interaction

    [Header("UI Prompt")]
    [SerializeField] GameObject promptRoot;
    [SerializeField] TextMeshProUGUI promptTMP;
    [SerializeField] string promptText = "Hold E to smell";

    [Header("HUD Tip")]
    [SerializeField] TipChipUI tipChip;
    [SerializeField] string qHintText = "[ Q ]  RE-SHOW TRAIL";

    [Header("Trail Timing")]
    public float showTrailSeconds = 3f; // how long the trail stays visible after interaction

    [Header("Sniff Audio")]
    public AudioSource sniffSource;
    public AudioClip sniffClip;
    [Range(0f, 1f)] public float sniffVolume = 0.7f;

    bool used;
    Transform player;
    CatController controller;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        player = p.transform;
        controller = p.GetComponent<CatController>();
        if (promptTMP) promptTMP.text = promptText;
        if (promptRoot) promptRoot.SetActive(false);
    }

    void Update()
    {
        if (used && oneTimeOnly) return; // already used, do nothing
        if (player == null || controller == null) return;

        bool inRange = Vector3.Distance(transform.position, player.position) <= interactRange;
        if (promptRoot) promptRoot.SetActive(inRange); // show prompt when close enough

        if (!inRange) return;
        if (!controller.ConsumeInteractPressed()) return; // wait for E press
        if (trailToUnlock == null) return;

        if (oneTimeOnly) used = true;
        if (particleToHide) particleToHide.SetActive(false); // hide the particle on the clue

        CurrentActiveTrail = trailToUnlock; // store as active trail so Q can re-show it
        trailToUnlock.UnlockAndShow();
        trailToUnlock.ShowForSeconds(showTrailSeconds);

        if (tipChip) tipChip.Pop(qHintText); // show the Q hint chip

        if (promptRoot) promptRoot.SetActive(false);

        if (sniffSource && sniffClip)
        {
            sniffSource.pitch = Random.Range(0.92f, 1.05f);
            sniffSource.PlayOneShot(sniffClip, sniffVolume); // play sniff sound
        }
    }
}