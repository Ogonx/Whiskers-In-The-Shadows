using UnityEngine;

public class ExitBlocker : MonoBehaviour
{
    public ScentClue requiredClue; // once this clue is used the blocker disables itself
    public AudioSource audioSource;
    public AudioClip meowClip;     // cat meows when blocked by this wall
    [Range(0f, 1f)] public float meowVolume = 0.8f;

    float cooldown; // prevents meow spam

    void Update()
    {
        if (requiredClue != null && requiredClue.IsUsed)
            gameObject.SetActive(false); // remove blocker once the required clue has been found

        if (cooldown > 0f) cooldown -= Time.deltaTime;
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        if (cooldown > 0f) return;
        if (audioSource && meowClip)
            audioSource.PlayOneShot(meowClip, meowVolume); // meow when cat hits the invisible wall
        cooldown = 3f; // wait 3 seconds before meowing again
    }
}