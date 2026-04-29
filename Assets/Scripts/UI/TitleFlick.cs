using UnityEngine;
using TMPro;

public class TitleFlick : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] TextMeshProUGUI titleText; // the title text to flicker

    [Header("Colors")]
    public Color bright = new Color(0.92f, 0.92f, 0.92f, 1f); // bright end of flicker
    public Color dark = new Color(0.12f, 0.12f, 0.12f, 1f);   // dark end of flicker

    [Header("Speed")]
    public float speed = 0.6f;

    [Header("Curve")]
    public float gamma = 1.2f; // higher gamma = spends more time at the dark end

    void Reset() => titleText = GetComponent<TextMeshProUGUI>();

    void Awake()
    {
        if (titleText == null) titleText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (titleText == null) return;
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        t = Mathf.Pow(t, gamma); // apply gamma curve to bias toward dark
        titleText.color = Color.Lerp(dark, bright, t); // flicker between dark and bright
    }
}