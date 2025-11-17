using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light lightSrc;
    public float minIntensity = 2f;   // NEVER below 1.5
    public float maxIntensity = 3.5f;

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * 5f, 0f);
        lightSrc.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
