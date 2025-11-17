using UnityEngine;

public class PSXCameraFollow : MonoBehaviour
{
    [Header("Target and Offset")]
    public Transform target; // Assign "Cat Model" here
    public Vector3 offset = new Vector3(0, 2, -5); // Offset behind and above
    public float smoothSpeed = 0.125f; // Smoothing for PS1 lag effect

    [Header("PS1 Effects")]
    public float panSpeed = 0.5f; // Subtle pan for unease
    private float panAngle = 0f;

    void LateUpdate()
    {
        // Calculate desired position with offset
        Vector3 desiredPosition = target.position + offset + GetPanOffset();
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Look at target with slight tilt
        transform.LookAt(target.position + Vector3.up * 0.5f); // Slight upward focus
    }

    Vector3 GetPanOffset()
    {
        // Simulate PS1 camera pan (slow oscillation)
        panAngle += panSpeed * Time.deltaTime;
        float panX = Mathf.Sin(panAngle) * 0.5f; // Small horizontal pan
        return new Vector3(panX, 0, 0);
    }

    // Optional: Add PS1 jitter effect
    void Update()
    {
        if (Random.value > 0.95f) // Random jitter (1% chance per frame)
        {
            transform.position += Random.insideUnitSphere * 0.01f; // Tiny random offset
        }
    }
}