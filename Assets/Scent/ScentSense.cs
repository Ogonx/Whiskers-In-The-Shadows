using UnityEngine;

public class ScentSense : MonoBehaviour
{
    public float showTime = 3f;
    public ScentTrail[] trails;

    private CatController controller;

    void Start()
    {
        controller = GetComponent<CatController>();
    }

    void Update()
    {
        if (controller == null) return;

        if (controller.ConsumeSensePressed())
        {
            foreach (var t in trails)
            {
                if (t != null)
                    t.ShowForSeconds(showTime);
            }
        }
    }
}
