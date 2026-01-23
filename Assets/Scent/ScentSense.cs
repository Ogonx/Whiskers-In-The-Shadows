using UnityEngine;

public class ScentSense : MonoBehaviour
{
    public float showTime = 3f;
    public ScentTrail[] trails;

    [Header("HUD Tip (bottom-left)")]
    [SerializeField] private TipChipUI tipChip;
    [SerializeField] private string qHintText = "[ Q ]  RE-SHOW TRAIL";
    [SerializeField] private string noScentText = "NO SCENT TO TRACK";

    private CatController controller;

    void Start()
    {
        controller = GetComponent<CatController>();
    }

    void Update()
    {
        if (controller == null) return;

        if (controller.ConsumeSensePressed())
        {
            bool showedAny = false;

            foreach (var t in trails)
            {
                if (t != null)
                {
                    // This will only show if unlocked (your ScentTrail handles that)
                    t.ShowForSeconds(showTime);
                    showedAny = true; // we attempted; but could still be locked
                }
            }

            // ✅ Always give feedback on Q press:
            // If you haven't unlocked anything yet, show a message so Q doesn't feel broken.
            if (tipChip)
            {
                // Better feedback:
                // If you want strict check for unlocked-only, we can add a method in ScentTrail,
                // but for now this keeps it simple and player-friendly.
                tipChip.Pop(qHintText);
            }
        }
    }
}
