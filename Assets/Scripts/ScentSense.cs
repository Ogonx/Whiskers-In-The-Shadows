using UnityEngine;

public class ScentSense : MonoBehaviour
{
    public float showTime = 3f; // how long the trail shows when Q is pressed
    public ScentTrail[] trails; // fallback trails shown if no active trail is set

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
        if (!controller.ConsumeSensePressed()) return; // wait for Q press

        if (ScentClue.CurrentActiveTrail != null)
        {
            ScentClue.CurrentActiveTrail.ShowForSeconds(showTime); // re-show the active trail
            if (tipChip) tipChip.Pop(qHintText);
            return;
        }

        // no active trail, try showing any assigned fallback trails
        bool showedAny = false;
        foreach (var t in trails)
        {
            if (t == null) continue;
            t.ShowForSeconds(showTime);
            showedAny = true;
        }

        if (tipChip) tipChip.Pop(showedAny ? qHintText : noScentText); // show appropriate hint
    }
}