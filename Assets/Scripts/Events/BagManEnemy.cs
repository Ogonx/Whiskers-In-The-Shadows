using System.Collections;
using UnityEngine;

public class BagManEnemy : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float runSpeed = 7.5f;
    [SerializeField] float turnSpeed = 10f;
    [SerializeField] string runBoolName = "IsRunning";

    bool rushing;
    Transform target;

    void Reset() => animator = GetComponentInChildren<Animator>();
    void Start()
    {
        if (animator) animator.SetBool(runBoolName, false);
    }

    void Update()
    {
        if (!rushing || target == null) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.1f)
        {
            StopRush();
            return;
        }

        Quaternion look = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * turnSpeed);
        transform.position += transform.forward * (runSpeed * Time.deltaTime);
    }

    public void RushToPoint(Transform point)
    {
        target = point;
        rushing = true;
        if (animator) animator.SetBool(runBoolName, true);
    }

    public void StopRush()
    {
        rushing = false;
        target = null;
        if (animator) animator.SetBool(runBoolName, false);
        gameObject.SetActive(false);
    }
}