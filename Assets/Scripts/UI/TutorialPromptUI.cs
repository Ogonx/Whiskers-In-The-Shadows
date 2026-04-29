using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialPromptUI : MonoBehaviour
{
    [Header("Messages")]
    public string[] messages = { "WASD to move", "Shift to run" }; // messages to display one after another
    public float displayTime = 5f;   // how long each message stays on screen
    public float fadeOutTime = 1.5f; // how long the fade out takes
    public float gapBetween = 0.5f;  // gap between messages

    [Header("Style")]
    public Color textColor = Color.white;
    public int fontSize = 48;
    public TMP_FontAsset pixelFont; // the pixel font used for the retro look

    CanvasGroup cg;
    TextMeshProUGUI tmp;

    public void Show()
    {
        Build();
        StartCoroutine(ShowMessages());
    }

    void Build()
    {
        // dynamically creates the canvas and text at runtime
        GameObject canvasObj = new GameObject("TutorialCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("TutorialPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.88f); // positioned near the top of the screen
        rt.anchorMax = new Vector2(0.5f, 0.88f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800f, 100f);
        rt.anchoredPosition = Vector2.zero;

        cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f; // start invisible

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
            yield return StartCoroutine(PixelReveal(msg)); // scramble then reveal the text

            yield return new WaitForSeconds(displayTime); // hold on screen

            // fade out
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

        Destroy(cg.transform.parent.parent.gameObject); // clean up canvas when done
    }

    IEnumerator PixelReveal(string msg)
    {
        cg.alpha = 1f;
        float revealTime = 0.6f;
        float elapsed = 0f;
        string chars = "█▓▒░#@%&*?!"; // scramble characters used before letters are revealed

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
                    display += msg[i]; // revealed character
                else
                    display += chars[Random.Range(0, chars.Length)]; // scrambled character
            }

            tmp.text = display;
            yield return null;
        }

        tmp.text = msg; // snap to final text
    }
}