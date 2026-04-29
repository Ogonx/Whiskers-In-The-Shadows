using UnityEngine;

public class CheckpointSystem : MonoBehaviour
{
    public static Vector3 SavedPosition;     // the last saved position of the cat
    public static Quaternion SavedRotation;  // the last saved rotation of the cat
    public static bool HasCheckpoint = false; // whether a checkpoint has been saved yet

    public static void Save(Vector3 pos, Quaternion rot)
    {
        SavedPosition = pos;
        SavedRotation = rot;
        HasCheckpoint = true; // mark that a checkpoint now exists
    }

    public static void LoadCheckpoint(CatController cat)
    {
        if (!HasCheckpoint) return;
        cat.transform.position = SavedPosition; // teleport cat to saved position
        cat.transform.rotation = SavedRotation; // restore saved rotation
    }
}