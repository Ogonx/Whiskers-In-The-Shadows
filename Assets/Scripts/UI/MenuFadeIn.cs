using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuFadeIn : MonoBehaviour
{
    [Header("Fade Image")]
    public Image fadeImage;
    [Header("Timing")]
    public float fadeDuration = 1.0f;

    bool busy; // prevents multiple fades running at once

    void Awake()
    {
        if (fadeImage == null)
            Debug.LogError("MenuFader: fadeImage not assigned.");
    }

    public void FadeOutFromBlack(float duration)
    {
        if (fadeImage == null) return;
        fadeImage.gameObject.SetActive(true);
        SetAlpha(1f);             // start fully black
        fadeDuration = duration;
        StartCoroutine(FadeTo(0f)); // fade to transparent
    }

    public IEnumerator FadeInToBlack(float duration)
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        fadeDuration = duration;
        yield return FadeTo(1f); // fade to fully black
    }

    IEnumerator FadeTo(float targetA)
    {
        if (busy) yield break;
        busy = true;

        float startA = fadeImage.color.a;
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startA, targetA, t / fadeDuration); // lerp alpha toward target
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetA;
        fadeImage.color = c;
        busy = false;
    }

    void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}