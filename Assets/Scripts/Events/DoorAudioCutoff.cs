using UnityEngine;

public class DoorAudioCutoff : MonoBehaviour
{
    [SerializeField] AudioSource clockSource;
    [SerializeField] AudioSource windSource;
    [SerializeField] float fadeSpeed = 5f;

    [Header("Clock")]
    [Range(0f, 1f)] public float clockInsideVolume = 0.65f;
    [Range(0f, 1f)] public float clockOutsideVolume = 0f;

    [Header("Wind")]
    [Range(0f, 1f)] public float windInsideVolume = 0.1f;
    [Range(0f, 1f)] public float windOutsideVolume = 0.8f;

    float targetClock;
    float targetWind;

    void Start()
    {
        targetClock = clockInsideVolume;
        targetWind = windInsideVolume;

        if (clockSource) clockSource.volume = clockInsideVolume;
        if (windSource) windSource.volume = windInsideVolume;
    }

    void Update()
    {
        if (clockSource) clockSource.volume = Mathf.Lerp(clockSource.volume, targetClock, Time.deltaTime * fadeSpeed);
        if (windSource) windSource.volume = Mathf.Lerp(windSource.volume, targetWind, Time.deltaTime * fadeSpeed);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        targetClock = clockInsideVolume;
        targetWind = windInsideVolume;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        targetClock = clockOutsideVolume;
        targetWind = windOutsideVolume;
    }
}