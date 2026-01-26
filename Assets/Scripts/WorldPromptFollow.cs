using UnityEngine;

public class WorldPromptFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;   // current interactable (laundry, bag, etc.)

    [Header("Offset / Motion")]
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    public float followSpeed = 15f;

    // Allow other scripts (ScentClue) to safely check who owns the prompt
    public Transform Target => target;

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        // Smooth follow
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );

        // Billboard to camera
        transform.rotation = Quaternion.LookRotation(
            transform.position - Camera.main.transform.position
        );
    }

    // Called by ScentClue when player enters range
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Optional helpers (clean + readable)
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        target = null;
    }
}
