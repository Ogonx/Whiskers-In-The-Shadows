using UnityEngine;

public class WorldPromptFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Offset / Motion")]
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    public float followSpeed = 15f;

    public Transform Target => target;

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        transform.position = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Show() => gameObject.SetActive(true);

    public void Hide()
    {
        gameObject.SetActive(false);
        target = null;
    }
}