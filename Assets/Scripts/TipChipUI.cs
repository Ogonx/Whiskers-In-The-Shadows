using System.Collections;
using UnityEngine;
using TMPro;

public class TipChipUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text tipText;

    [Header("Timing")]
    [SerializeField] private float duration = 4f;  // how long the chip stays visible
    [SerializeField] private float fadeOut = 0.25f; // how long the fade out takes

    private Coroutine co;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        HideNow();
    }

    public void Pop(string message)
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (tipText) tipText.text = message;
        if (co != null) StopCoroutine(co); // cancel any existing show
        co = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        canvasGroup.alpha = 1f; // show immediately
        yield return new WaitForSeconds(duration);

        // fade out
        float t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeOut);
            yield return null;
        }

        HideNow();
        co = null;
    }

    private void HideNow()
    {
        if (canvasGroup) canvasGroup.alpha = 0f; // invisible
    }
}