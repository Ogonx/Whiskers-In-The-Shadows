using System.Collections;
using UnityEngine;
using TMPro;

public class TipChipUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text tipText;

    [Header("Timing")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private float fadeOut = 0.25f;

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

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // IMPORTANT: do NOT deactivate the GameObject
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

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
        if (canvasGroup) canvasGroup.alpha = 0f;
    }
}
