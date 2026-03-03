using UnityEngine;
using TMPro;

public class TitleFlick : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] TextMeshProUGUI titleText;

    [Header("Colors")]
    public Color bright = new Color(0.92f, 0.92f, 0.92f, 1f);
    public Color dark = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Header("Speed")]
    public float speed = 0.6f;

    [Header("Curve")]
    public float gamma = 1.2f;

    void Reset() => titleText = GetComponent<TextMeshProUGUI>();

    void Awake()
    {
        if (titleText == null) titleText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (titleText == null) return;

        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        t = Mathf.Pow(t, gamma);
        titleText.color = Color.Lerp(dark, bright, t);
    }
}