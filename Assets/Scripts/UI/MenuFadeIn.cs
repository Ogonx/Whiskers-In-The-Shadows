using UnityEngine;
using UnityEngine.UI;

public class MenuFadeIn : MonoBehaviour
{
    [Header("Assign the full-screen black Image")]
    public Image fadeImage;

    [Header("Timing")]
    public float delay = 0.1f;      // small pause before fading
    public float fadeDuration = 1.5f;

    [Header("Optional: disable overlay after fade")]
    public bool disableAfterFade = true;

    void Start()
    {
        if (fadeImage == null)
        {
            Debug.LogError("MenuFadeIn: fadeImage not assigned.");
            return;
        }

        // Start fully black
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        // Begin fade
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // unaffected by timescale
            float a = 1f - Mathf.Clamp01(t / fadeDuration);
            c.a = a;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;

        if (disableAfterFade)
            fadeImage.gameObject.SetActive(false);
    }
}
