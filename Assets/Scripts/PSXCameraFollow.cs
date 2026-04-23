using UnityEngine;
public class PSXCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    public float distance = 4f;
    public float height = 2f;
    public float followSpeed = 5f;
    public float rotateSpeed = 8f;

    [Header("Chase Mode")]
    public Transform chaseTarget;
    public float chaseDistance = 6f;
    public float chaseHeight = 2.5f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float sphereRadius = 0.3f;
    public float wallOffset = 0.08f;
    public float minDistance = 0.8f;
    public float collisionPushSpeed = 20f;
    public float collisionReturnSpeed = 10f;

    [HideInInspector] public bool frontMode = false;
    [HideInInspector] public bool frozen = false;
    [HideInInspector] public bool blendingBack = false;
    [HideInInspector] public float blendBackTimer = 0f;
    [HideInInspector] public float blendBackDuration = 1.5f;
    [HideInInspector] public Quaternion blendStartRot;
    [HideInInspector] public Vector3 blendStartPos;

    float currentDistance;

    void Start()
    {
        currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null || frozen) return;

        if (frontMode)
        {
            if (chaseTarget != null)
            {
                Vector3 dirFromBagMan = (target.position - chaseTarget.position);
                dirFromBagMan.y = 0f;
                dirFromBagMan.Normalize();

                Vector3 frontPos = target.position + dirFromBagMan * chaseDistance + Vector3.up * chaseHeight;
                transform.position = Vector3.Lerp(transform.position, frontPos, followSpeed * Time.deltaTime);
            }

            Vector3 lookDir = (target.position + Vector3.up * 1f) - transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookDir), rotateSpeed * 2f * Time.deltaTime);
            return;
        }

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

        if (blendingBack)
        {
            blendBackTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, blendBackTimer / blendBackDuration);
            transform.position = Vector3.Lerp(blendStartPos, finalPos, t);
            transform.rotation = Quaternion.Slerp(blendStartRot, Quaternion.LookRotation(target.position - transform.position), t);
            if (blendBackTimer >= blendBackDuration) blendingBack = false;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, finalPos, followSpeed * Time.deltaTime);
        Quaternion lookRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }
}