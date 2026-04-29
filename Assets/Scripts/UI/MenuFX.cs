using UnityEngine;
using UnityEngine.UI;

public class MenuFX : MonoBehaviour
{
    [Header("UI Layers")]
    public RawImage fogBG; // scrolling fog texture in the background
    public RawImage noiseOverlay; // flickering noise overlay on top

    [Header("Fog")]
    public Vector2 fogScrollSpeed = new Vector2(0.003f, 0.001f); // how fast the fog UV scrolls

    [Header("Noise")]
    [Range(0f, 0.5f)] public float noiseBaseAlpha = 0.12f;      // base opacity of the noise
    [Range(0f, 0.5f)] public float noiseFlickerAmount = 0.05f;  // how much the noise flickers
    public float noiseFlickerSpeed = 8f;

    [Header("Wobble")]
    public RectTransform wobbleRoot; // the UI element that gently wobbles
    public float wobbleAmount = 0.4f;
    public float wobbleSpeed = 1.2f;

    void Update()
    {
        // scroll fog UV coordinates each frame
        if (fogBG != null)
        {
            Rect r = fogBG.uvRect;
            r.position += fogScrollSpeed * Time.unscaledDeltaTime;
            fogBG.uvRect = r;
        }

        // flicker noise overlay opacity using perlin noise
        if (noiseOverlay != null)
        {
            float flicker = (Mathf.PerlinNoise(Time.unscaledTime * noiseFlickerSpeed, 0f) - 0.5f) * 2f;
            Color c = noiseOverlay.color;
            c.a = Mathf.Clamp01(noiseBaseAlpha + flicker * noiseFlickerAmount);
            noiseOverlay.color = c;
        }

        // gently move wobble root using perlin noise for organic movement
        if (wobbleRoot != null)
        {
            float x = (Mathf.PerlinNoise(Time.unscaledTime * wobbleSpeed, 1.1f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(Time.unscaledTime * wobbleSpeed, 2.2f) - 0.5f) * 2f;
            wobbleRoot.anchoredPosition = new Vector2(x, y) * wobbleAmount;
        }
    }
}