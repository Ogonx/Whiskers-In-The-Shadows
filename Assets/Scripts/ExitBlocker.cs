using UnityEngine;

public class ExitBlocker : MonoBehaviour
{
    public ScentClue requiredClue;
    public AudioSource audioSource;
    public AudioClip meowClip;
    [Range(0f, 1f)] public float meowVolume = 0.8f;
    float cooldown;

    void Update()
    {
        if (requiredClue != null && requiredClue.IsUsed)
            gameObject.SetActive(false);

        if (cooldown > 0f) cooldown -= Time.deltaTime;
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        if (cooldown > 0f) return;

        if (audioSource && meowClip)
            audioSource.PlayOneShot(meowClip, meowVolume);

        cooldown = 3f;
    }
}