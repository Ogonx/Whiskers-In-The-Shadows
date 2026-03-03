using UnityEngine;

public class PSXCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    public float distance = 4f;
    public float height = 2f;
    public float followSpeed = 5f;
    public float rotateSpeed = 8f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float sphereRadius = 0.3f;
    public float wallOffset = 0.08f;
    public float minDistance = 0.8f;
    public float collisionPushSpeed = 20f;
    public float collisionReturnSpeed = 10f;

    float currentDistance;

    void Start()
    {
        currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position - target.forward * distance + Vector3.up * height;
        Vector3 origin = target.position + Vector3.up * (height * 0.5f);
        Vector3 toDesired = desiredPos - origin;
        float desiredDist = toDesired.magnitude;

        if (desiredDist > 0.001f)
        {
            Vector3 dir = toDesired / desiredDist;
            float targetDist = distance;

            if (Physics.SphereCast(origin, sphereRadius, dir, out RaycastHit hit, desiredDist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float hitDist = Mathf.Max(minDistance, hit.distance - wallOffset);
                float ratio = hitDist / desiredDist;
                targetDist = Mathf.Lerp(minDistance, distance, ratio);
                currentDistance = Mathf.MoveTowards(currentDistance, targetDist, collisionPushSpeed * Time.deltaTime);
            }
            else
            {
                currentDistance = Mathf.MoveTowards(currentDistance, distance, collisionReturnSpeed * Time.deltaTime);
            }
        }

        Vector3 finalPos = target.position - target.forward * currentDistance + Vector3.up * height;

        transform.position = Vector3.Lerp(transform.position, finalPos, followSpeed * Time.deltaTime);

        Quaternion lookRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }
}