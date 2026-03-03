using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light lightSrc;
    public float minIntensity = 2f;
    public float maxIntensity = 3.5f;

    void Update()
    {
        lightSrc.intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PerlinNoise(Time.time * 5f, 0f));
    }
}