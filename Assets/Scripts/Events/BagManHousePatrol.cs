using UnityEngine;
using System.Collections;

public class BagManHousePatrol : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;  // the points BagMan walks between inside the house
    public float waitAtPoint = 3f;    // how long he pauses at each point

    [Header("Movement")]
    public float walkSpeed = 2f;      // walking speed
    public float turnSpeed = 8f;      // how quickly he rotates to face his direction
    public float arrivalDistance = 2f; // how close he needs to get before moving to next point

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepVolume = 0.8f;
    public float footstepInterval = 0.6f;           // time between each footstep sound
    [Range(0.1f, 1f)] public float footstepPitch = 0.6f; // pitch of the footstep sound

    [Header("Animator")]
    public string walkBoolName = "IsWalking"; // animator parameter name for walking

    Animator animator;
    int currentPoint = 0;      // which patrol point he is heading to
    int patrolDirection = 1;   // 1 = forward through list, -1 = backward
    bool patrolling = false;   // whether patrol is active
    [HideInInspector] public bool isWalking = false; // used by FootstepRoutine to know when to play sounds

    void Start()
    {
        animator = GetComponent<Animator>();
        gameObject.SetActive(false); // starts hidden, activated by StartPatrol
    }

    public void StartPatrol()
    {
        if (patrolling) return; // already patrolling, dont start again
        gameObject.SetActive(true);
        patrolling = true;
        StartCoroutine(PatrolRoutine());
        StartCoroutine(FootstepRoutine());
    }

    IEnumerator FootstepRoutine()
    {
        while (patrolling)
        {
            if (isWalking)
            {
                if (footstepSource && footstepClip)
                {
                    footstepSource.pitch = footstepPitch;
                    footstepSource.PlayOneShot(footstepClip, footstepVolume); // play one footstep
                }
                yield return new WaitForSeconds(footstepInterval); // wait before next footstep
            }
            else
            {
                if (footstepSource) footstepSource.Stop(); // stop footsteps when standing still
                yield return null;
            }
        }
    }

    IEnumerator PatrolRoutine()
    {
        while (patrolling)
        {
            if (patrolPoints.Length == 0) yield break; // no points set, stop

            Transform target = patrolPoints[currentPoint];

            if (animator) animator.SetBool(walkBoolName, true);
            isWalking = true;

            // move toward the current patrol point
            while (Vector3.Distance(transform.position, target.position) > arrivalDistance)
            {
                Vector3 dir = (target.position - transform.position);
                dir.y = 0f;
                dir.Normalize();

                transform.position += dir * walkSpeed * Time.deltaTime; // move forward

                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * turnSpeed // smoothly rotate to face direction of travel
                    );

                yield return null;
            }

            if (animator) animator.SetBool(walkBoolName, false);
            isWalking = false;

            yield return new WaitForSeconds(waitAtPoint); // pause at this point

            // move to next point, ping pong back and forth through the list
            currentPoint += patrolDirection;
            if (currentPoint >= patrolPoints.Length - 1)
                patrolDirection = -1; // reached the end, start going backward
            else if (currentPoint <= 0)
                patrolDirection = 1;  // reached the start, go forward again
        }
    }
}