using UnityEngine;
using UnityEngine.UI;

public class MenuFX : MonoBehaviour
{
    [Header("UI Layers")]
    public RawImage fogBG;
    public RawImage noiseOverlay;

    [Header("Fog")]
    public Vector2 fogScrollSpeed = new Vector2(0.003f, 0.001f);

    [Header("Noise")]
    [Range(0f, 0.5f)] public float noiseBaseAlpha = 0.12f;
    [Range(0f, 0.5f)] public float noiseFlickerAmount = 0.05f;
    public float noiseFlickerSpeed = 8f;

    [Header("Wobble")]
    public RectTransform wobbleRoot;
    public float wobbleAmount = 0.4f;
    public float wobbleSpeed = 1.2f;

    void Update()
    {
        if (fogBG != null)
        {
            Rect r = fogBG.uvRect;
            r.position += fogScrollSpeed * Time.unscaledDeltaTime;
            fogBG.uvRect = r;
        }

        if (noiseOverlay != null)
        {
            float flicker = (Mathf.PerlinNoise(Time.unscaledTime * noiseFlickerSpeed, 0f) - 0.5f) * 2f;
            Color c = noiseOverlay.color;
            c.a = Mathf.Clamp01(noiseBaseAlpha + flicker * noiseFlickerAmount);
            noiseOverlay.color = c;
        }

        if (wobbleRoot != null)
        {
            float x = (Mathf.PerlinNoise(Time.unscaledTime * wobbleSpeed, 1.1f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(Time.unscaledTime * wobbleSpeed, 2.2f) - 0.5f) * 2f;
            wobbleRoot.anchoredPosition = new Vector2(x, y) * wobbleAmount;
        }
    }
}