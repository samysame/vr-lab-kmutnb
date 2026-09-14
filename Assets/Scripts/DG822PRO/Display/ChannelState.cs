using UnityEngine;

/// <summary>
/// Holds the simulated parameters of a single generator channel (CH1 or CH2).
/// </summary>
[System.Serializable]
public class ChannelState
{
    public WaveformType waveform = WaveformType.Sine;
    public float frequencyHz = 1000f;
    public float amplitudeVpp = 2f;
    public float offsetV = 0f;
    public float phaseDeg = 0f;
    public bool outputOn = false;
}
