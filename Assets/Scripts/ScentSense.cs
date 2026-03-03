using UnityEngine;

public class ScentSense : MonoBehaviour
{
    public float showTime = 3f;
    public ScentTrail[] trails;

    [Header("HUD Tip")]
    [SerializeField] TipChipUI tipChip;
    [SerializeField] string qHintText = "[ Q ]  RE-SHOW TRAIL";
    [SerializeField] string noScentText = "NO SCENT TO TRACK";

    CatController controller;

    void Start()
    {
        controller = GetComponent<CatController>();
    }

    void Update()
    {
        if (controller == null) return;
        if (!controller.ConsumeSensePressed()) return;

        if (ScentClue.CurrentActiveTrail != null)
        {
            ScentClue.CurrentActiveTrail.ShowForSeconds(showTime);
            if (tipChip) tipChip.Pop(qHintText);
            return;
        }

        bool showedAny = false;
        foreach (var t in trails)
        {
            if (t == null) continue;
            t.ShowForSeconds(showTime);
            showedAny = true;
        }

        if (tipChip) tipChip.Pop(showedAny ? qHintText : noScentText);
    }
}