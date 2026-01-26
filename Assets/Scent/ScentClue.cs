using UnityEngine;
using TMPro;

public class ScentClue : MonoBehaviour
{
    // ✅ Global "current scent" without a manager script
    public static ScentTrail CurrentActiveTrail;

    [Header("Interaction")]
    public float interactRange = 2f;
    public bool oneTimeOnly = true;

    [Header("What this clue unlocks")]
    public ScentTrail trailToUnlock;

    [Header("UI (Local prompt above THIS object)")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private string promptText = "Hold E to smell";

    [Header("HUD Tip (bottom-left)")]
    [SerializeField] private TipChipUI tipChip;
    [SerializeField] private string qHintText = "[ Q ]  RE-SHOW TRAIL";

    [Header("Trail Timing")]
    public float showTrailSeconds = 3f;

    private bool used = false;
    private Transform player;
    private CatController controller;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            controller = p.GetComponent<CatController>();
        }

        if (promptTMP != null)
            promptTMP.text = promptText;

        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    void Update()
    {
        if (used && oneTimeOnly) return;
        if (player == null || controller == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= interactRange;

        if (promptRoot != null)
            promptRoot.SetActive(inRange);

        if (!inRange) return;

        if (controller.ConsumeInteractPressed())
        {
            if (trailToUnlock == null) return;

            if (oneTimeOnly) used = true;

            // ✅ Make THIS the active trail for Q
            CurrentActiveTrail = trailToUnlock;

            trailToUnlock.UnlockAndShow();
            trailToUnlock.ShowForSeconds(showTrailSeconds);

            if (tipChip) tipChip.Pop(qHintText);

            if (promptRoot != null)
                promptRoot.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
