using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [Tooltip("If empty, will use Camera.main")]
    public Camera cam;

    [Tooltip("Keep the prompt upright (recommended)")]
    public bool lockYAxis = true;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = transform.position - cam.transform.position;

        if (lockYAxis)
            dir.y = 0f;

        // Face the camera (this direction matters!)
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
