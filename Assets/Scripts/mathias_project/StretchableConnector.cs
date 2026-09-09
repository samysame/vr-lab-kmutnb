using UnityEngine;

/// <summary>
/// Generic component that stretches and orients its GameObject to span
/// between two world-space points, non-uniformly scaling along a single
/// configurable axis. Used for both wires and resistors in the two-click
/// placement system - same math, different prefab/mesh underneath.
/// </summary>
public class StretchableConnector : MonoBehaviour
{
    private enum LengthAxis { X, Y, Z }

    [Tooltip("Which local axis of this mesh runs along its length (check the model in Blender/Unity to confirm - the resistor's long axis was Y in the original .blend file, but Unity's FBX import may remap it).")]
    [SerializeField] private LengthAxis lengthAxis = LengthAxis.Z;

    [Tooltip("The object's length along lengthAxis at localScale = 1, in Unity units (meters). Measure this once per prefab (drop it in a scene, check its bounds in the Inspector) and set it here - do not guess.")]
    [SerializeField] private float referenceLength = 1f;

    /// <summary>
    /// Positions this object at the midpoint between a and b, orients it
    /// so its length axis points from a to b, and scales it (along that
    /// axis only) to exactly span the distance between them.
    /// </summary>
    public void PlaceBetween(Vector3 a, Vector3 b)
    {
        Vector3 midpoint = (a + b) * 0.5f;
        Vector3 direction = b - a;
        float distance = direction.magnitude;

        if (distance < 0.0001f)
        {
            Debug.LogWarning($"{name}: start and end points are the same - skipping placement.", this);
            return;
        }

        transform.position = midpoint;
        transform.rotation = RotationForAxis(direction.normalized);

        float scaleFactor = referenceLength > 0f ? distance / referenceLength : 1f;
        Vector3 scale = transform.localScale;
        switch (lengthAxis)
        {
            case LengthAxis.X: scale.x = scaleFactor; break;
            case LengthAxis.Y: scale.y = scaleFactor; break;
            case LengthAxis.Z: scale.z = scaleFactor; break;
        }
        transform.localScale = scale;
    }

    private Quaternion RotationForAxis(Vector3 direction)
    {
        // Aligns whichever local axis is the "length" axis with the
        // given world-space direction.
        switch (lengthAxis)
        {
            case LengthAxis.X: return Quaternion.FromToRotation(Vector3.right, direction);
            case LengthAxis.Y: return Quaternion.FromToRotation(Vector3.up, direction);
            default: return Quaternion.FromToRotation(Vector3.forward, direction);
        }
    }
}
