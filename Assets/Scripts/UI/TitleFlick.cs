using UnityEngine;
using TMPro;

public class TitleFlick : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Breathing Colors (inside)")]
    public Color bright = new Color(0.92f, 0.92f, 0.92f, 1f); // near-white
    public Color dark = new Color(0.12f, 0.12f, 0.12f, 1f); // near-black

    [Header("Speed")]
    public float speed = 0.6f; // slow = 0.3–0.8

    [Header("Curve feel")]
    public float gamma = 1.2f; // 1 = linear, >1 feels more “moody”

    void Reset()
    {
        titleText = GetComponent<TextMeshProUGUI>();
    }

    void Awake()
    {
        if (titleText == null)
            titleText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (titleText == null) return;

        // 0..1..0 smooth loop
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;

        // make it feel less “mathy”
        t = Mathf.Pow(t, gamma);

        // Only changes face color; outline/underlay stays from your TMP material
        titleText.color = Color.Lerp(dark, bright, t);
    }
}
