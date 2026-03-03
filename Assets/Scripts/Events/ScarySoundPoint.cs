using UnityEngine;

public class ScarySoundPoint : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Audio")]
    public float minDistance = 3f;
    public float maxDistance = 15f;
    [Range(0f, 1f)] public float volume = 0.7f;
    [SerializeField] Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    AudioSource src;
    bool played;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
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

        src.pitch = Random.Range(pitchRange.x, pitchRange.y);
        src.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
    }
}