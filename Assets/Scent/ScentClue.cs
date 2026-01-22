using UnityEngine;
using TMPro;

public class ScentClue : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 2f;
    public bool oneTimeOnly = true;

    [Header("What this clue unlocks")]
    public ScentTrail trailToUnlock;

    [Header("UI")]
    public TextMeshProUGUI interactText;

    [Header("Trail Timing")]
    public float showTrailSeconds = 3f;

    private bool used = false;
    private GameObject player;
    private CatController controller;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            controller = player.GetComponent<CatController>();

        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (used && oneTimeOnly) return;

        if (player == null || controller == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);

        bool inRange = dist <= interactRange;

        // Show / hide UI prompt
        if (interactText != null)
            interactText.gameObject.SetActive(inRange);

        if (!inRange) return;

        // Press E to sniff
        if (controller.ConsumeInteractPressed())
        {
            if (trailToUnlock == null) return;

            if (oneTimeOnly) used = true;

            trailToUnlock.UnlockAndShow();
            trailToUnlock.ShowForSeconds(showTrailSeconds);

            // Hide prompt after interaction
            if (interactText != null)
                interactText.gameObject.SetActive(false);
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
