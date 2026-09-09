using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// C# equivalent of SimulationLibrary.cpp from the Unreal prototype.
/// Sends circuit parameters to the local FastAPI backend and returns
/// the computed voltage/current/power/mode.
///
/// Usage (from any MonoBehaviour):
///   CircuitSimulationClient.Instance.Simulate(
///       voltage: 5f, maxCurrent: 1f, resistance: 1000f,
///       onSuccess: result => Debug.Log(result.voltage),
///       onError: error => Debug.LogError(error));
/// </summary>
public class CircuitSimulationClient : MonoBehaviour
{
    private const string BaseUrl = "http://127.0.0.1:8000";
    private const float RequestTimeoutSeconds = 5f;

    private static CircuitSimulationClient _instance;

    /// <summary>
    /// Lazily creates a hidden GameObject to host this client the first
    /// time it's needed, so callers don't have to manually place it in
    /// every scene.
    /// </summary>
    public static CircuitSimulationClient Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CircuitSimulationClient");
                _instance = go.AddComponent<CircuitSimulationClient>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Serializable]
    private class SimulationRequest
    {
        public float voltage;
        public float max_current;
        public float resistance;
    }

    /// <summary>
    /// Mirrors the JSON returned by the backend's /simulate endpoint.
    /// Field names must match exactly for JsonUtility to parse them.
    /// </summary>
    [Serializable]
    public class SimulationResult
    {
        public float voltage;
        public float current;
        public float resistance;
        public float power;
        public string mode; // "CV" or "CC"
    }

    /// <summary>
    /// Sends a POST /simulate request. Both callbacks are optional but
    /// at least one should be provided to do anything useful with the
    /// result.
    /// </summary>
    public void Simulate(float voltage, float maxCurrent, float resistance,
        Action<SimulationResult> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(SimulateCoroutine(voltage, maxCurrent, resistance, onSuccess, onError));
    }

    private IEnumerator SimulateCoroutine(float voltage, float maxCurrent, float resistance,
        Action<SimulationResult> onSuccess, Action<string> onError)
    {
        var payload = new SimulationRequest
        {
            voltage = voltage,
            max_current = maxCurrent,
            resistance = resistance
        };
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest($"{BaseUrl}/simulate", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = Mathf.CeilToInt(RequestTimeoutSeconds);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            // Covers both network errors (backend not running) and
            // HTTP error codes (e.g. 500 from a failed ngspice run).
            string message = $"Simulation request failed: {request.error}";
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                message += $" ({request.downloadHandler.text})";
            }
            Debug.LogWarning(message);
            onError?.Invoke(message);
            yield break;
        }

        SimulationResult result;
        try
        {
            result = JsonUtility.FromJson<SimulationResult>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            string message = $"Could not parse simulation response: {e.Message}";
            Debug.LogWarning(message);
            onError?.Invoke(message);
            yield break;
        }

        onSuccess?.Invoke(result);
    }
}
