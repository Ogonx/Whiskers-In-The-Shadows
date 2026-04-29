using UnityEngine;

public class PSXCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;      // the cat
    public float distance = 4f;   // default follow distance behind the cat
    public float height = 2f;     // default follow height above the cat
    public float followSpeed = 5f;
    public float rotateSpeed = 8f;

    [Header("Chase Mode")]
    public Transform chaseTarget;    // BagMan's transform, used in front mode
    public float chaseDistance = 6f; // distance in front of cat in front mode
    public float chaseHeight = 2.5f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float sphereRadius = 0.3f;    // radius of the collision sphere cast
    public float wallOffset = 0.08f;     // how far to keep camera from walls
    public float minDistance = 0.8f;     // closest the camera can get to the cat
    public float collisionPushSpeed = 20f;  // how fast camera moves toward cat when hitting geometry
    public float collisionReturnSpeed = 10f; // how fast camera returns to normal distance

    [HideInInspector] public bool frontMode = false;    // when true camera positions in front of cat to show BagMan chasing
    [HideInInspector] public bool frozen = false;       // when true camera stops updating entirely
    [HideInInspector] public bool blendingBack = false; // when true camera smoothly returns to normal follow
    [HideInInspector] public float blendBackTimer = 0f;
    [HideInInspector] public float blendBackDuration = 1.5f;
    [HideInInspector] public Quaternion blendStartRot;
    [HideInInspector] public Vector3 blendStartPos;

    float currentDistance; // actual current follow distance, adjusted by collision

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
                // position camera on the opposite side of the cat from BagMan
                Vector3 dirFromBagMan = (target.position - chaseTarget.position);
                dirFromBagMan.y = 0f;
                dirFromBagMan.Normalize();
                Vector3 frontPos = target.position + dirFromBagMan * chaseDistance + Vector3.up * chaseHeight;
                transform.position = Vector3.Lerp(transform.position, frontPos, followSpeed * Time.deltaTime);
            }

            // always look at the cat in front mode
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

            // sphere cast to check for geometry between cat and desired camera position
            if (Physics.SphereCast(origin, sphereRadius, dir, out RaycastHit hit, desiredDist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float hitDist = Mathf.Max(minDistance, hit.distance - wallOffset);
                float ratio = hitDist / desiredDist;
                targetDist = Mathf.Lerp(minDistance, distance, ratio);
                currentDistance = Mathf.MoveTowards(currentDistance, targetDist, collisionPushSpeed * Time.deltaTime); // push camera toward cat
            }
            else
            {
                currentDistance = Mathf.MoveTowards(currentDistance, distance, collisionReturnSpeed * Time.deltaTime); // return to normal distance
            }
        }

        Vector3 finalPos = target.position - target.forward * currentDistance + Vector3.up * height;

        if (blendingBack)
        {
            blendBackTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, blendBackTimer / blendBackDuration);
            transform.position = Vector3.Lerp(blendStartPos, finalPos, t);
            transform.rotation = Quaternion.Slerp(blendStartRot, Quaternion.LookRotation(target.position - transform.position), t);
            if (blendBackTimer >= blendBackDuration) blendingBack = false; // blend complete
            return;
        }

        // normal follow
        transform.position = Vector3.Lerp(transform.position, finalPos, followSpeed * Time.deltaTime);
        Quaternion lookRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }
}