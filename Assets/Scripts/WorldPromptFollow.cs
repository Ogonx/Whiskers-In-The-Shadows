using UnityEngine;

public class WorldPromptFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target; // the transform this prompt follows

    [Header("Offset / Motion")]
    public Vector3 offset = new Vector3(0, 1.2f, 0); // height offset above the target
    public float followSpeed = 15f;

    public Transform Target => target; // read-only access for other scripts

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        transform.position = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime); // smoothly follow target

        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position); // always face the camera
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