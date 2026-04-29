using UnityEngine;

public class JumpscareTriggerGate : MonoBehaviour
{
    public ScentClue axeClue; // the trigger only activates once this clue has been used

    Collider trigger;

    void Start()
    {
        trigger = GetComponent<Collider>();
        trigger.enabled = false; // starts disabled
    }

    void Update()
    {
        if (axeClue != null && axeClue.IsUsed)
            trigger.enabled = true; // enable trigger once the player has interacted with the clue
    }
}