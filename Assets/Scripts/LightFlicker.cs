using UnityEngine;

public class SoftFlicker : MonoBehaviour
{
    public Light lightSource;
    public float minIntensity = 0.6f;
    public float maxIntensity = 1.2f;
    public float speed = 2.5f;

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * speed, 0.0f);
        lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
