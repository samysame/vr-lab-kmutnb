using UnityEngine;

/// <summary>
/// Pure math helpers to evaluate a waveform shape.
/// Kept separate from DisplayController so it stays easy to unit test
/// or reuse (e.g. for an audio preview later on).
/// </summary>
public static class WaveformMath
{
    /// <summary>
    /// Returns a normalized value in [-1, 1] for a given phase in [0, 1).
    /// </summary>
    public static float Evaluate(WaveformType type, float phase01)
    {
        phase01 = Mathf.Repeat(phase01, 1f);

        switch (type)
        {
            case WaveformType.Sine:
                return Mathf.Sin(phase01 * Mathf.PI * 2f);

            case WaveformType.Square:
                return phase01 < 0.5f ? 1f : -1f;

            case WaveformType.Ramp:
                return (phase01 * 2f) - 1f;

            default:
                return 0f;
        }
    }
}
