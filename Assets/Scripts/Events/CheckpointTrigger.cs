using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        CheckpointSystem.Save(other.transform.position, other.transform.rotation); // save current position as checkpoint
        gameObject.SetActive(false); // disable after firing so it only saves once
    }
}