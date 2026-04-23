using UnityEngine;

public class CheckpointSystem : MonoBehaviour
{
    public static Vector3 SavedPosition;
    public static Quaternion SavedRotation;
    public static bool HasCheckpoint = false;

    public static void Save(Vector3 pos, Quaternion rot)
    {
        SavedPosition = pos;
        SavedRotation = rot;
        HasCheckpoint = true;
    }

    public static void LoadCheckpoint(CatController cat)
    {
        if (!HasCheckpoint) return;
        cat.transform.position = SavedPosition;
        cat.transform.rotation = SavedRotation;
    }
}