using UnityEngine;

public class ForestZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var cat = other.GetComponent<CatController>();
        if (cat) cat.SetInForest(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var cat = other.GetComponent<CatController>();
        if (cat) cat.SetInForest(false);
    }
}