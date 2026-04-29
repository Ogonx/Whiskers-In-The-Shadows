using UnityEngine;

public class ScarySoundPoint : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] clips; // random clip picked from this array each time

    [Header("Audio")]
    public float minDistance = 3f;   // full volume within this distance
    public float maxDistance = 15f;  // silent beyond this distance
    [Range(0f, 1f)] public float volume = 0.7f;
    [SerializeField] Vector2 pitchRange = new Vector2(0.9f, 1.1f); // slight pitch randomisation each play

    AudioSource src;
    bool played; // only plays once per trigger entry

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 1f;                           // fully 3D
        src.rolloffMode = AudioRolloffMode.Logarithmic;  // natural falloff
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.playOnAwake = false;
        src.loop = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (played) return;
        played = true;
        if (clips == null || clips.Length == 0) return;
        src.pitch = Random.Range(pitchRange.x, pitchRange.y); // randomise pitch slightly
        src.PlayOneShot(clips[Random.Range(0, clips.Length)], volume); // play a random clip
    }
}