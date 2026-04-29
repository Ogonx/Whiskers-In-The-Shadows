using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PawStamp : MonoBehaviour
{
    [Header("Assign")]
    public Image pawImage; // the paw print sprite

    [Header("Positions")]
    public List<Vector2> positions = new List<Vector2>(); // screen positions for each paw stamp

    [Header("Angles")]
    public List<float> angles = new List<float>(); // rotation for each stamp

    [Header("Scales")]
    public List<float> scales = new List<float>(); // scale for each stamp

    [Header("Timing")]
    public float delayBetween = 0.4f; // time between each paw print appearing

    public IEnumerator Play()
    {
        if (pawImage == null) yield break;

        gameObject.SetActive(true);
        pawImage.gameObject.SetActive(true);

        var c = pawImage.color;
        c.a = 1f;
        pawImage.color = c;

        for (int i = 0; i < positions.Count; i++)
        {
            pawImage.rectTransform.anchoredPosition = positions[i]; // move to next paw position
            pawImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i < angles.Count ? angles[i] : 0f);
            pawImage.rectTransform.localScale = Vector3.one * (i < scales.Count ? scales[i] : 1f);
            yield return new WaitForSecondsRealtime(delayBetween); // wait before showing next paw
        }
    }
}