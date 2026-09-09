using UnityEngine;

/// <summary>
/// Drives the two-click placement flow: select a first ConnectionPoint,
/// then a second one, and a wire or resistor prefab is spawned and
/// stretched between them via StretchableConnector.
///
/// For this POC, the "tool" (wire vs resistor) is switched with number
/// keys for quick testing without headset (matches the Device Simulator
/// workflow used earlier). Wiring this to a proper in-VR tool menu is a
/// planned follow-up, not part of this pass.
/// </summary>
public class TwoClickPlacementController : MonoBehaviour
{
    public enum PlacementTool { Wire, Resistor }

    [Header("Prefabs (must have a StretchableConnector component)")]
    [SerializeField] private StretchableConnector wirePrefab;
    [SerializeField] private StretchableConnector resistorPrefab;

    private static TwoClickPlacementController _instance;
    public static TwoClickPlacementController Instance => _instance;

    private PlacementTool _currentTool = PlacementTool.Wire;
    private ConnectionPoint _firstSelection;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Multiple TwoClickPlacementController instances found - keeping the first one.", this);
            Destroy(this);
            return;
        }
        _instance = this;
    }

    private void Update()
    {
        // Keyboard shortcuts for testing without a headset. Replace with
        // a proper VR tool-select UI later.
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTool(PlacementTool.Wire);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTool(PlacementTool.Resistor);
        if (Input.GetKeyDown(KeyCode.Escape)) CancelSelection();
    }

    public void SetTool(PlacementTool tool)
    {
        _currentTool = tool;
        CancelSelection();
        Debug.Log($"Placement tool set to {tool}");
    }

    /// <summary>
    /// Called by ConnectionPoint.Select() when the player selects a
    /// connection point with the ray interactor.
    /// </summary>
    public void OnConnectionPointSelected(ConnectionPoint point)
    {
        if (_firstSelection == null)
        {
            _firstSelection = point;
            Debug.Log($"First connection point selected: {point.role}. Select the second point to place a {_currentTool}.");
            return;
        }

        if (_firstSelection == point)
        {
            Debug.Log("Same point selected twice - ignoring. Select a different second point, or press Escape to cancel.");
            return;
        }

        PlaceConnector(_firstSelection, point);
        _firstSelection = null;
    }

    public void CancelSelection()
    {
        if (_firstSelection != null)
        {
            Debug.Log("Selection cancelled.");
            _firstSelection = null;
        }
    }

    private void PlaceConnector(ConnectionPoint from, ConnectionPoint to)
    {
        StretchableConnector prefab = _currentTool == PlacementTool.Wire ? wirePrefab : resistorPrefab;
        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for tool {_currentTool} on {name}.", this);
            return;
        }

        StretchableConnector instance = Instantiate(prefab);
        instance.PlaceBetween(from.transform.position, to.transform.position);
        Debug.Log($"Placed {_currentTool} between \"{from.role}\" and \"{to.role}\".");
    }
}
