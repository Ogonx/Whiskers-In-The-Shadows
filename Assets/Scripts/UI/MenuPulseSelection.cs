using UnityEngine;
using TMPro;

public class MenuPulseSelection : MonoBehaviour
{
    [Header("Menu Items (Top -> Bottom)")]
    public TextMeshProUGUI[] items;

    [Header("Selection")]
    public int selectedIndex = 0;

    [Header("Pulse")]
    public Color brightColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    public Color darkColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public float pulseSpeed = 2.2f; // Silent Hill vibe: 1.6–2.8

    [Header("Unselected")]
    public Color unselectedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    void Update()
    {
        if (items == null || items.Length == 0) return;

        // Pulse value 0..1..0
        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            if (i == selectedIndex)
            {
                items[i].color = Color.Lerp(darkColor, brightColor, t);
            }
            else
            {
                items[i].color = unselectedColor;
            }
        }
    }

    // Call these from your input/menu script:
    public void SetSelected(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, items.Length - 1);
    }
}
