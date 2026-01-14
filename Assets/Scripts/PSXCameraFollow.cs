using UnityEngine;

public class BehindFollowCamera : MonoBehaviour
{
    public Transform target;         // the cat
    public float distance = 4f;      // how far behind
    public float height = 2f;        // how high above
    public float followSpeed = 5f;   // movement smoothing
    public float rotateSpeed = 8f;   // rotation smoothing

    void LateUpdate()
    {
        if (target == null) return;

        // Desired camera position (behind the cat)
        Vector3 behindPos = target.position
                            - target.forward * distance
                            + Vector3.up * height;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, behindPos, followSpeed * Time.deltaTime);

        // Smooth rotation to look at the cat
        Quaternion lookRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }
}
