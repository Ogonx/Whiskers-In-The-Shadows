using System.Collections;
using UnityEngine;

public class BagManEnemy : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float runSpeed = 7.5f;   // how fast BagMan moves toward the target
    [SerializeField] float turnSpeed = 10f;   // how quickly he rotates to face the target
    [SerializeField] string runBoolName = "IsRunning"; // animator parameter name for running

    bool rushing;      // whether BagMan is currently rushing toward a target
    Transform target;  // the transform BagMan is rushing toward

    void Reset() => animator = GetComponentInChildren<Animator>(); // auto-assign animator in editor

    void Start()
    {
        if (animator) animator.SetBool(runBoolName, false); // make sure he starts idle
    }

    void Update()
    {
        if (!rushing || target == null) return; // not rushing or no target, do nothing

        Vector3 dir = target.position - transform.position;
        dir.y = 0f; // keep movement flat so he doesnt tilt

        if (dir.sqrMagnitude < 0.1f)
        {
            StopRush(); // close enough to target, stop rushing
            return;
        }

        Quaternion look = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * turnSpeed); // smoothly rotate to face target
        transform.position += transform.forward * (runSpeed * Time.deltaTime); // move forward
    }

    public void RushToPoint(Transform point)
    {
        target = point;   // set the target to rush toward
        rushing = true;
        if (animator) animator.SetBool(runBoolName, true); // play run animation
    }

    public void StopRush()
    {
        rushing = false;
        target = null;
        if (animator) animator.SetBool(runBoolName, false); // stop run animation
    }

    public void Hide()
    {
        gameObject.SetActive(false); // completely disable BagMan's GameObject
    }
}