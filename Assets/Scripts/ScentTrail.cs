using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScentTrail : MonoBehaviour
{
    [Header("Trail Setup")]
    public List<Transform> points = new List<Transform>();
    public GameObject scentParticlePrefab;
    public float spacing = 0.8f;

    [Header("Timing")]
    public float visibleTime = 3f;

    [Header("Fade Out")]
    public float fadeOutTime = 1.0f;
    public bool randomiseFade = true;
    public Vector2 fadeVariation = new Vector2(0.8f, 1.2f);

    List<GameObject> spawned = new List<GameObject>();
    List<Vector3> originalScales = new List<Vector3>();
    Dictionary<GameObject, Coroutine> fadeRoutines = new Dictionary<GameObject, Coroutine>();

    Coroutine hideRoutine;
    bool unlocked;

    void Start()
    {
        BuildTrail();
        SetTrailVisible(false, instant: true);
    }

    void BuildTrail()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);

        spawned.Clear();
        originalScales.Clear();
        fadeRoutines.Clear();

        if (scentParticlePrefab == null || points.Count < 2) return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            if (points[i] == null || points[i + 1] == null) continue;

            Vector3 a = points[i].position;
            Vector3 b = points[i + 1].position;

            int count = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / spacing));

            for (int j = 0; j <= count; j++)
            {
                var obj = Instantiate(scentParticlePrefab, Vector3.Lerp(a, b, (float)j / count), Quaternion.identity, transform);
                spawned.Add(obj);
                originalScales.Add(obj.transform.localScale);
                obj.SetActive(false);
            }
        }
    }

    public void UnlockAndShow()
    {
        unlocked = true;
        ShowTrail();
    }

    public void ShowTrail()
    {
        if (!unlocked) return;

        if (hideRoutine != null) StopCoroutine(hideRoutine);

        SetTrailVisible(true, instant: true);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    public void ShowForSeconds(float seconds)
    {
        if (!unlocked) return;

        StopAllCoroutines();
        SetTrailVisible(true, instant: true);
        StartCoroutine(ShowRoutine(seconds));
    }

    IEnumerator ShowRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetTrailVisible(false, instant: false);
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleTime);
        SetTrailVisible(false, instant: false);
    }

    void SetTrailVisible(bool visible, bool instant)
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            var go = spawned[i];
            if (go == null) continue;

            if (visible)
            {
                StopFade(go);
                go.SetActive(true);
                go.transform.localScale = originalScales[i];
            }
            else if (instant || fadeOutTime <= 0.001f)
            {
                StopFade(go);
                go.SetActive(false);
            }
            else
            {
                StartFade(go, originalScales[i]);
            }
        }
    }

    void StartFade(GameObject go, Vector3 startScale)
    {
        StopFade(go);

        float duration = fadeOutTime;
        if (randomiseFade) duration *= Random.Range(fadeVariation.x, fadeVariation.y);

        fadeRoutines[go] = StartCoroutine(FadeByScaleRoutine(go, startScale, duration));
    }

    void StopFade(GameObject go)
    {
        if (fadeRoutines.TryGetValue(go, out var co) && co != null)
            StopCoroutine(co);

        fadeRoutines.Remove(go);
    }

    IEnumerator FadeByScaleRoutine(GameObject go, Vector3 startScale, float duration)
    {
        if (go == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            if (go == null) yield break;

            t += Time.deltaTime;
            go.transform.localScale = startScale * Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(t / duration));
            yield return null;
        }

        go.transform.localScale = Vector3.zero;
        go.SetActive(false);
        fadeRoutines.Remove(go);
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Trail")]
    void RebuildTrailEditor()
    {
        BuildTrail();
        SetTrailVisible(false, instant: true);
    }
#endif
}