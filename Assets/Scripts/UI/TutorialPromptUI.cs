using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialPromptUI : MonoBehaviour
{
    [Header("Messages")]
    public string[] messages = { "WASD to move", "Shift to run" };
    public float displayTime = 5f;
    public float fadeOutTime = 1.5f;
    public float gapBetween = 0.5f;

    [Header("Style")]
    public Color textColor = Color.white;
    public int fontSize = 48;
    public TMP_FontAsset pixelFont;

    CanvasGroup cg;
    TextMeshProUGUI tmp;

    public void Show()
    {
        Build();
        StartCoroutine(ShowMessages());
    }

    void Build()
    {
        GameObject canvasObj = new GameObject("TutorialCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("TutorialPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.88f);
        rt.anchorMax = new Vector2(0.5f, 0.88f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800f, 100f);
        rt.anchoredPosition = Vector2.zero;

        cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        GameObject textObj = new GameObject("TutorialText");
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 8f;
        if (pixelFont) tmp.font = pixelFont;
    }

    IEnumerator ShowMessages()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (string msg in messages)
        {
            yield return StartCoroutine(PixelReveal(msg));

            yield return new WaitForSeconds(displayTime);

            float t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
                yield return null;
            }
            cg.alpha = 0f;

            yield return new WaitForSeconds(gapBetween);
        }

        Destroy(cg.transform.parent.parent.gameObject);
    }

    IEnumerator PixelReveal(string msg)
    {
        cg.alpha = 1f;
        float revealTime = 0.6f;
        float elapsed = 0f;
        string chars = "█▓▒░#@%&*?!";

        while (elapsed < revealTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / revealTime;
            int revealedCount = Mathf.RoundToInt(progress * msg.Length);

            string display = "";
            for (int i = 0; i < msg.Length; i++)
            {
                if (msg[i] == ' ')
                    display += ' ';
                else if (i < revealedCount)
                    display += msg[i];
                else
                    display += chars[Random.Range(0, chars.Length)];
            }

            tmp.text = display;
            yield return null;
        }

        tmp.text = msg;
    }
}