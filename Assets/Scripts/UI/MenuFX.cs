using UnityEngine;
using UnityEngine.UI;

public class SilentHillMenuFX : MonoBehaviour
{
    [Header("UI Layers")]
    public RawImage fogBG;
    public RawImage noiseOverlay;

    [Header("Fog Movement")]
    public Vector2 fogScrollSpeed = new Vector2(0.003f, 0.001f);

    [Header("Noise Flicker")]
    [Range(0f, 0.5f)] public float noiseBaseAlpha = 0.12f;
    [Range(0f, 0.5f)] public float noiseFlickerAmount = 0.05f;
    public float noiseFlickerSpeed = 8f;

    [Header("Screen Wobble")]
    public RectTransform wobbleRoot;
    public float wobbleAmount = 0.4f;
    public float wobbleSpeed = 1.2f;

    void Update()
    {
        // Fog slow UV scroll
        if (fogBG != null)
        {
            Rect r = fogBG.uvRect;
            r.position += fogScrollSpeed * Time.unscaledDeltaTime;
            fogBG.uvRect = r;
        }

        // Noise alpha flicker
        if (noiseOverlay != null)
        {
            float flicker =
                (Mathf.PerlinNoise(Time.unscaledTime * noiseFlickerSpeed, 0f) - 0.5f) * 2f;

            float a = Mathf.Clamp01(noiseBaseAlpha + flicker * noiseFlickerAmount);
            Color c = noiseOverlay.color;
            c.a = a;
            noiseOverlay.color = c;
        }

        // Tiny screen wobble (CRT instability)
        if (wobbleRoot != null)
        {
            float x = (Mathf.PerlinNoise(Time.unscaledTime * wobbleSpeed, 1.1f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(Time.unscaledTime * wobbleSpeed, 2.2f) - 0.5f) * 2f;

            wobbleRoot.anchoredPosition = new Vector2(x, y) * wobbleAmount;
        }
    }
}
