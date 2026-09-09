using UnityEngine;

/// <summary>
/// Marks a fixed connection point (power supply output/return, resistor
/// terminal) that the player can select with the ray interactor as part
/// of the two-click wire/resistor placement flow.
///
/// This POC only needs 2-3 of these (power supply output, resistor
/// terminal(s), power supply return) - not a full breadboard socket
/// grid. That's a planned future iteration, not part of this pass.
///
/// Setup in the Unity Editor (no code required beyond this file):
///   1. Add a Collider to this GameObject (a small Sphere Collider works well).
///   2. Add an "XR Simple Interactable" component (from XR Interaction Toolkit).
///   3. In XR Simple Interactable's "Interactable Events" > "Select Entered",
///      add an entry that calls this component's Select() method.
/// This keeps this script decoupled from the exact XRI API/namespace,
/// which can shift between package versions.
/// </summary>
public class ConnectionPoint : MonoBehaviour
{
    [Tooltip("Human-readable role, useful for debugging and later for mapping to circuit parameters (e.g. \"PowerOutput\", \"PowerReturn\", \"ResistorTerminalA\").")]
    public string role;

    /// <summary>
    /// Called by the XR Simple Interactable's Select Entered event
    /// (wired up in the Inspector - see class comment above).
    /// </summary>
    public void Select()
    {
        TwoClickPlacementController.Instance.OnConnectionPointSelected(this);
    }
}
