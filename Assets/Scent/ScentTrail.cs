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

    private List<GameObject> spawned = new List<GameObject>();
    private Coroutine hideRoutine;
    private bool unlocked = false;

    void Start()
    {
        BuildTrail();
        SetTrailVisible(false);
    }

    void BuildTrail()
    {
        foreach (var go in spawned)
            Destroy(go);
        spawned.Clear();

        if (scentParticlePrefab == null || points.Count < 2) return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i].position;
            Vector3 b = points[i + 1].position;
            float dist = Vector3.Distance(a, b);
            int count = Mathf.Max(1, Mathf.RoundToInt(dist / spacing));

            for (int j = 0; j <= count; j++)
            {
                float t = (float)j / count;
                Vector3 p = Vector3.Lerp(a, b, t);
                var obj = Instantiate(scentParticlePrefab, p, Quaternion.identity, transform);
                spawned.Add(obj);
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

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        SetTrailVisible(true);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    public void ShowForSeconds(float seconds)
    {
        if (!unlocked) return;

        StopAllCoroutines();
        StartCoroutine(ShowRoutine(seconds));
    }

    private System.Collections.IEnumerator ShowRoutine(float seconds)
    {
        SetTrailVisible(true);
        yield return new WaitForSeconds(seconds);
        SetTrailVisible(false);
    }


    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleTime);
        SetTrailVisible(false);
    }

    void SetTrailVisible(bool visible)
    {
        foreach (var go in spawned)
            if (go != null)
                go.SetActive(visible);
    }
}
