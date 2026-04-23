using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScentClue : MonoBehaviour
{
    public static ScentTrail CurrentActiveTrail;

    public bool IsUsed => used;

    [Header("Interaction")]
    public float interactRange = 2f;
    public bool oneTimeOnly = true;

    [Header("What this clue unlocks")]
    public ScentTrail trailToUnlock;

    [Header("Particle")]
    public GameObject particleToHide;

    [Header("UI Prompt")]
    [SerializeField] GameObject promptRoot;
    [SerializeField] TextMeshProUGUI promptTMP;
    [SerializeField] string promptText = "Hold E to smell";

    [Header("HUD Tip")]
    [SerializeField] TipChipUI tipChip;
    [SerializeField] string qHintText = "[ Q ]  RE-SHOW TRAIL";

    [Header("Trail Timing")]
    public float showTrailSeconds = 3f;

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
        if (used && oneTimeOnly) return;
        if (player == null || controller == null) return;

        bool inRange = Vector3.Distance(transform.position, player.position) <= interactRange;
        if (promptRoot) promptRoot.SetActive(inRange);
        if (!inRange) return;
        if (!controller.ConsumeInteractPressed()) return;
        if (trailToUnlock == null) return;

        if (oneTimeOnly) used = true;

        if (particleToHide) particleToHide.SetActive(false);

        CurrentActiveTrail = trailToUnlock;
        trailToUnlock.UnlockAndShow();
        trailToUnlock.ShowForSeconds(showTrailSeconds);

        if (tipChip) tipChip.Pop(qHintText);
        if (promptRoot) promptRoot.SetActive(false);

        if (sniffSource && sniffClip)
        {
            sniffSource.pitch = Random.Range(0.92f, 1.05f);
            sniffSource.PlayOneShot(sniffClip, sniffVolume);
        }
    }
}