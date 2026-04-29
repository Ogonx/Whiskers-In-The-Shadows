using UnityEngine;
using TMPro;

public class MenuPulseSelection : MonoBehaviour
{
    [Header("Menu Items")]
    public TextMeshProUGUI[] items; // all the selectable text items

    [Header("Selection")]
    public int selectedIndex = 0;

    [Header("Pulse")]
    public Color brightColor = new Color(0.92f, 0.92f, 0.92f, 1f); // bright end of pulse
    public Color darkColor = new Color(0.15f, 0.15f, 0.15f, 1f);   // dark end of pulse
    public float pulseSpeed = 2.2f;

    [Header("Unselected")]
    public Color unselectedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    void Update()
    {
        if (items == null || items.Length == 0) return;

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0 to 1 sine wave

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            items[i].color = i == selectedIndex
                ? Color.Lerp(darkColor, brightColor, t) // selected item pulses
                : unselectedColor;                      // others stay dim
        }
    }

    public void SetSelected(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, items.Length - 1);
    }
}