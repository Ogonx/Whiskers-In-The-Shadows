using UnityEngine;

public class DoorAudioCutoff : MonoBehaviour
{
    [SerializeField] AudioSource clockSource;
    [SerializeField] AudioSource windSource;
    [SerializeField] float fadeSpeed = 5f; // how quickly volumes change between indoor and outdoor targets

    [Header("Clock")]
    [Range(0f, 1f)] public float clockInsideVolume = 0.65f;  // clock volume when inside
    [Range(0f, 1f)] public float clockOutsideVolume = 0f;    // clock volume when outside

    [Header("Wind")]
    [Range(0f, 1f)] public float windInsideVolume = 0.1f;    // wind volume when inside
    [Range(0f, 1f)] public float windOutsideVolume = 0.8f;   // wind volume when outside

    float targetClock; // current target volume for the clock
    float targetWind;  // current target volume for the wind

    void Start()
    {
        targetClock = clockInsideVolume;
        targetWind = windInsideVolume;
        if (clockSource) clockSource.volume = clockInsideVolume; // start at indoor volumes
        if (windSource) windSource.volume = windInsideVolume;
    }

    void Update()
    {
        // smoothly move toward target volumes every frame
        if (clockSource) clockSource.volume = Mathf.Lerp(clockSource.volume, targetClock, Time.deltaTime * fadeSpeed);
        if (windSource) windSource.volume = Mathf.Lerp(windSource.volume, targetWind, Time.deltaTime * fadeSpeed);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        targetClock = clockInsideVolume; // player is inside, use indoor volumes
        targetWind = windInsideVolume;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        targetClock = clockOutsideVolume; // player has left, use outdoor volumes
        targetWind = windOutsideVolume;
    }
}