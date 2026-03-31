using UnityEngine;
using System.Collections;

public class BagManHousePatrol : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float waitAtPoint = 3f;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float turnSpeed = 8f;
    public float arrivalDistance = 2f;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepVolume = 0.8f;
    public float footstepInterval = 0.6f;
    [Range(0.1f, 1f)] public float footstepPitch = 0.6f;

    [Header("Animator")]
    public string walkBoolName = "IsWalking";

    Animator animator;
    int currentPoint = 0;
    int patrolDirection = 1;
    bool patrolling = false;
    [HideInInspector] public bool isWalking = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        gameObject.SetActive(false);
    }

    public void StartPatrol()
    {
        if (patrolling) return;
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
                    footstepSource.PlayOneShot(footstepClip, footstepVolume);
                }
                yield return new WaitForSeconds(footstepInterval);
            }
            else
            {
                if (footstepSource) footstepSource.Stop();
                yield return null;
            }
        }
    }

    IEnumerator PatrolRoutine()
    {
        while (patrolling)
        {
            if (patrolPoints.Length == 0) yield break;

            Transform target = patrolPoints[currentPoint];

            if (animator) animator.SetBool(walkBoolName, true);
            isWalking = true;

            while (Vector3.Distance(transform.position, target.position) > arrivalDistance)
            {
                Vector3 dir = (target.position - transform.position);
                dir.y = 0f;
                dir.Normalize();

                transform.position += dir * walkSpeed * Time.deltaTime;

                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * turnSpeed
                    );

                yield return null;
            }

            if (animator) animator.SetBool(walkBoolName, false);
            isWalking = false;

            yield return new WaitForSeconds(waitAtPoint);

            currentPoint += patrolDirection;

            if (currentPoint >= patrolPoints.Length - 1)
                patrolDirection = -1;
            else if (currentPoint <= 0)
                patrolDirection = 1;
        }
    }
}