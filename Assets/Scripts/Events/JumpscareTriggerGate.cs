using UnityEngine;

public class JumpscareTriggerGate : MonoBehaviour
{
    public ScentClue axeClue;
    Collider trigger;

    void Start()
    {
        trigger = GetComponent<Collider>();
        trigger.enabled = false;
    }

    void Update()
    {
        if (axeClue != null && axeClue.IsUsed)
            trigger.enabled = true;
    }
}