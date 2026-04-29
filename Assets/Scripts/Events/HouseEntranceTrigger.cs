using UnityEngine;
using System.Collections;

public class HouseEntranceTrigger : MonoBehaviour
{
    public BagManHousePatrol bagManPatrol; // starts BagMan patrolling inside when triggered
    public AudioSource groanSource;        // ambient groan audio activated on entry
    public AudioSource windSource;         // wind faded out on entry
    public float windFadeDuration = 2f;
    public GameObject mansionExitBlocker; // invisible wall blocking exit until narrative allows it

    bool triggered = false;

    void Start()
    {
        if (groanSource) groanSource.enabled = false;  // groan starts disabled
        if (mansionExitBlocker) mansionExitBlocker.SetActive(false); // exit blocker starts disabled
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;

        if (bagManPatrol) bagManPatrol.StartPatrol(); // start BagMan walking inside the house

        if (groanSource)
        {
            groanSource.enabled = true;
            groanSource.Play(); // start the ambient groan
        }

        if (mansionExitBlocker) mansionExitBlocker.SetActive(true); // block the exit

        StartCoroutine(FadeOutWind());
    }

    IEnumerator FadeOutWind()
    {
        if (windSource == null) yield break;
        float startVolume = windSource.volume;
        float elapsed = 0f;
        while (elapsed < windFadeDuration)
        {
            elapsed += Time.deltaTime;
            windSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / windFadeDuration); // fade wind out
            yield return null;
        }
        windSource.volume = 0f;
        windSource.Stop();
    }
}