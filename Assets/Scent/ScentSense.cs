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
            // ✅ Priority: re-show the last smelled/unlocked trail
            if (ScentClue.CurrentActiveTrail != null)
            {
                ScentClue.CurrentActiveTrail.ShowForSeconds(showTime);

                if (tipChip) tipChip.Pop(qHintText);
                return;
            }

            // Fallback: try any listed trails (optional)
            bool showedAny = false;
            foreach (var t in trails)
            {
                if (t == null) continue;
                t.ShowForSeconds(showTime);
                showedAny = true;
            }

            if (tipChip)
                tipChip.Pop(showedAny ? qHintText : noScentText);
        }
    }
}
