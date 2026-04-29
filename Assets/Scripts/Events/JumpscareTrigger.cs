using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    [SerializeField] BagManJumpscareDirector director; // the jumpscare sequence to play
    [SerializeField] string playerTag = "Player";

    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        triggered = true;
        director.Play(); // fire the jumpscare director
    }
}