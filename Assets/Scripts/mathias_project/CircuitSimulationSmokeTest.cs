using UnityEngine;

/// <summary>
/// Attach this to any GameObject in a test scene to sanity-check the
/// connection to the backend without needing a headset or any of the
/// interaction logic yet. Press Play, then press Space in the Game
/// view - the result (or error) shows up in the Console.
///
/// Delete or disable once the real multimeter/probe logic replaces it.
/// </summary>
public class CircuitSimulationSmokeTest : MonoBehaviour
{
    [Header("Test parameters (sent to /simulate on key press)")]
    [SerializeField] private float testVoltage = 5f;
    [SerializeField] private float testMaxCurrent = 1f;
    [SerializeField] private float testResistance = 1000f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Sending test simulation request...");
            CircuitSimulationClient.Instance.Simulate(
                testVoltage, testMaxCurrent, testResistance,
                onSuccess: result => Debug.Log(
                    $"Simulation OK -> voltage={result.voltage}V, current={result.current}A, " +
                    $"power={result.power}W, mode={result.mode}"),
                onError: error => Debug.LogError($"Simulation failed: {error}")
            );
        }
    }
}
