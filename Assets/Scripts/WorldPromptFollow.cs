using UnityEngine;

public class WorldPromptFollow : MonoBehaviour
{
    public Transform target;       // the laundry
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    public float followSpeed = 15f;

    void LateUpdate()
    {
        if (target == null || Camera.main == null) return;

        // Follow the target smoothly
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );

        // Always face the camera
        transform.rotation = Quaternion.LookRotation(
            transform.position - Camera.main.transform.position
        );
    }
}
