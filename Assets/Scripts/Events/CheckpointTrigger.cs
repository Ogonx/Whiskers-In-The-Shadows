using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        CheckpointSystem.Save(other.transform.position, other.transform.rotation);
        gameObject.SetActive(false);
    }
}