using UnityEngine;

public class HouseZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var cat = other.GetComponent<CatController>();
        if (cat) cat.SetInHouse(true); // tell CatController it is inside, switches to hard footsteps
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var cat = other.GetComponent<CatController>();
        if (cat) cat.SetInHouse(false);
    }
}