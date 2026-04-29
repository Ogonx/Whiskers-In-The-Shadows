using UnityEngine;

public class BagManChase : MonoBehaviour
{
    public Transform[] waypoints; // the path BagMan follows during the chase
    public float runSpeed = 6f;   // how fast he moves between waypoints
    public Animator animator;     // controls the run animation

    int currentIndex = 0; // which waypoint BagMan is currently heading toward
    bool active = false;  // whether BagMan is currently chasing

    void Update()
    {
        if (!active) return;                 // not chasing, do nothing
        if (waypoints.Length == 0) return;  // no waypoints set, do nothing

        Transform target = waypoints[currentIndex]; // get the current target waypoint

        Vector3 dir = target.position - transform.position;
        dir.y = 0f; // ignore vertical difference so BagMan doesnt tilt up or down

        transform.position = Vector3.MoveTowards(transform.position, target.position, runSpeed * Time.deltaTime); // move toward waypoint

        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir); // face the direction of movement

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            currentIndex++; // reached this waypoint, move to the next one
            if (currentIndex >= waypoints.Length)
                currentIndex = waypoints.Length - 1; // stay at the last waypoint when the path ends
        }
    }

    public void StartChase()
    {
        active = true;
        if (animator) animator.SetBool("IsRunning", true); // play run animation
    }

    public void StopChase()
    {
        active = false;
        if (animator) animator.SetBool("IsRunning", false); // stop run animation
    }
}