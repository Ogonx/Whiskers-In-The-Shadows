using UnityEngine;

public class ScentClue : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 2f;
    public bool oneTimeOnly = true;

    [Header("What this clue unlocks")]
    public ScentTrail trailToUnlock;

    [Header("Show trail behaviour")]
    public float showTrailSeconds = 3f;   // how long the trail stays visible after sniff
    public bool showDebugLogs = true;

    private bool used = false;

    // Cache the player so we don't Find every frame
    private GameObject player;
    private CatController controller;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            controller = player.GetComponent<CatController>();
    }

    void Update()
    {
        if (used && oneTimeOnly) return;

        // If player wasn't found yet (scene loads etc), try again
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) controller = player.GetComponent<CatController>();
        }

        if (player == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("No Player found. Tag your cat as Player.");
            return;
        }

        if (controller == null)
        {
            controller = player.GetComponent<CatController>();
            if (controller == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("Player has no CatController component.");
                return;
            }
        }

        // Range check
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist > interactRange) return;

        if (showDebugLogs)
            Debug.Log("In range of clue: " + gameObject.name);

        // Press E to sniff
        if (controller.ConsumeInteractPressed())
        {
            if (trailToUnlock == null)
            {
                Debug.LogWarning("trailToUnlock is NOT assigned!");
                return;
            }

            if (oneTimeOnly) used = true;

            // Make sure the trail is unlocked, then show it briefly
            trailToUnlock.UnlockAndShow();
            trailToUnlock.ShowForSeconds(showTrailSeconds);


            if (showDebugLogs)
                Debug.Log("SNIFFED clue: " + gameObject.name + " (showing trail for " + showTrailSeconds + "s)");
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
