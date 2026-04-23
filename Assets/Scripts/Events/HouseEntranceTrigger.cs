using UnityEngine;
using System.Collections;

public class HouseEntranceTrigger : MonoBehaviour
{
    public BagManHousePatrol bagManPatrol;
    public AudioSource groanSource;
    public AudioSource windSource;
    public float windFadeDuration = 2f;
    public GameObject mansionExitBlocker;

    bool triggered = false;

    void Start()
    {
        if (groanSource) groanSource.enabled = false;
        if (mansionExitBlocker) mansionExitBlocker.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        if (bagManPatrol) bagManPatrol.StartPatrol();
        if (groanSource)
        {
            groanSource.enabled = true;
            groanSource.Play();
        }
        if (mansionExitBlocker) mansionExitBlocker.SetActive(true);
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
            windSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / windFadeDuration);
            yield return null;
        }
        windSource.volume = 0f;
        windSource.Stop();
    }
}