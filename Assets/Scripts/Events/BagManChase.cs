using UnityEngine;

public class BagManChase : MonoBehaviour
{
    public Transform[] waypoints;
    public float runSpeed = 6f;
    public Animator animator;

    int currentIndex = 0;
    bool active = false;

    void Update()
    {
        if (!active) return;
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentIndex];
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        transform.position = Vector3.MoveTowards(transform.position, target.position, runSpeed * Time.deltaTime);
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
                currentIndex = waypoints.Length - 1;
        }
    }

    public void StartChase()
    {
        active = true;
        if (animator) animator.SetBool("IsRunning", true);
    }

    public void StopChase()
    {
        active = false;
        if (animator) animator.SetBool("IsRunning", false);
    }
}