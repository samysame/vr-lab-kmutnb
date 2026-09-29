using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI references for a single channel panel (CH1 or CH2), mirroring the
/// real DG822/DG852 Pro screen: waveform name, big frequency readout,
/// amplitude/offset row, phase row, and a small waveform preview icon.
/// </summary>
[System.Serializable]
public class ChannelPanelRefs
{
    public TMP_Text waveformNameText;
    public TMP_Text frequencyText;
    public TMP_Text amplitudeText;
    public TMP_Text offsetText;
    public TMP_Text phaseText;
    public TMP_Text outputStateText;
    public RawImage waveformIcon;
}

/// <summary>
/// Drives the DG822/DG852 Pro screen (a World Space Canvas placed over the screen mesh).
/// Both channels are displayed simultaneously, side by side, like the real device.
/// Shows/hides the whole canvas based on MachineState.OnPowerChanged.
/// </summary>
public class DisplayController : MonoBehaviour
{
    [Header("Canvas root (hidden when the machine is off)")]
    public GameObject canvasRoot;

    [Header("One panel per channel, left to right (CH1, CH2)")]
    public ChannelPanelRefs[] panels = new ChannelPanelRefs[2] { new ChannelPanelRefs(), new ChannelPanelRefs() };

    [Header("Waveform icon size (small preview, not a full graph)")]
    public int iconWidth = 64;
    public int iconHeight = 36;
    public Color backgroundColor = new Color(0.02f, 0.02f, 0.02f);
    public Color[] channelTraceColors =
    {
        new Color(1f, 0.85f, 0f),   // CH1 - yellow
        new Color(0.2f, 0.85f, 1f)  // CH2 - cyan
    };

    [Header("Channels")]
    public ChannelState[] channels = new ChannelState[2] { new ChannelState(), new ChannelState() };

    /// <summary> Which channel the physical parameter buttons currently act on. </summary>
    public int SelectedChannel { get; private set; } = 0;

    Texture2D[] iconTextures;

    void Start()
    {
        iconTextures = new Texture2D[channels.Length];
        for (int i = 0; i < channels.Length; i++)
        {
            iconTextures[i] = new Texture2D(iconWidth, iconHeight, TextureFormat.RGB24, false);
            iconTextures[i].filterMode = FilterMode.Point;
            panels[i].waveformIcon.texture = iconTextures[i];
        }

        if (MachineState.Instance != null)
        {
            MachineState.Instance.OnPowerChanged += HandlePowerChanged;
            HandlePowerChanged(MachineState.Instance.IsPoweredOn);
        }
        else
        {
            HandlePowerChanged(false);
        }
    }

    void OnDestroy()
    {
        if (MachineState.Instance != null)
            MachineState.Instance.OnPowerChanged -= HandlePowerChanged;
    }

    void HandlePowerChanged(bool powerState)
    {
        canvasRoot.SetActive(powerState);
        if (powerState)
            RefreshAllPanels();
    }

    // ---------------- Public API, called by physical buttons ----------------

    public void SelectChannel(int channelIndex)
    {
        SelectedChannel = Mathf.Clamp(channelIndex, 0, channels.Length - 1);
    }

    public void SetWaveform(WaveformType type)
    {
        channels[SelectedChannel].waveform = type;
        RefreshPanel(SelectedChannel);
    }

    public void AdjustFrequency(float deltaHz)
    {
        ChannelState ch = channels[SelectedChannel];
        ch.frequencyHz = Mathf.Max(1f, ch.frequencyHz + deltaHz);
        RefreshPanel(SelectedChannel);
    }

    public void AdjustAmplitude(float deltaVpp)
    {
        ChannelState ch = channels[SelectedChannel];
        ch.amplitudeVpp = Mathf.Clamp(ch.amplitudeVpp + deltaVpp, 0.01f, 10f);
        RefreshPanel(SelectedChannel);
    }

    public void AdjustOffset(float deltaV)
    {
        ChannelState ch = channels[SelectedChannel];
        ch.offsetV += deltaV;
        RefreshPanel(SelectedChannel);
    }

    public void AdjustPhase(float deltaDeg)
    {
        ChannelState ch = channels[SelectedChannel];
        ch.phaseDeg = Mathf.Repeat(ch.phaseDeg + deltaDeg, 360f);
        RefreshPanel(SelectedChannel);
    }

    public void ToggleOutput(int channelIndex)
    {
        ChannelState ch = channels[channelIndex];
        ch.outputOn = !ch.outputOn;
        RefreshPanel(channelIndex);
    }

    // ---------------- UI refresh ----------------

    void RefreshAllPanels()
    {
        for (int i = 0; i < channels.Length; i++)
            RefreshPanel(i);
    }

    void RefreshPanel(int channelIndex)
    {
        ChannelState ch = channels[channelIndex];
        ChannelPanelRefs panel = panels[channelIndex];

        panel.waveformNameText.text = ch.waveform.ToString();
        panel.frequencyText.text = FormatFrequency(ch.frequencyHz);
        panel.amplitudeText.text = ch.amplitudeVpp.ToString("0.00") + " Vpp";
        panel.offsetText.text = ch.offsetV.ToString("0.00") + " Vdc";
        panel.phaseText.text = ch.phaseDeg.ToString("0.00") + " °";
        panel.outputStateText.text = ch.outputOn ? "ON" : "OFF";

        DrawWaveformIcon(channelIndex, ch);
    }

    string FormatFrequency(float hz)
    {
        if (hz >= 1000000f) return (hz / 1000000f).ToString("0.000") + " MHz";
        if (hz >= 1000f) return (hz / 1000f).ToString("0.000") + " kHz";
        return hz.ToString("0.00") + " Hz";
    }

    void DrawWaveformIcon(int channelIndex, ChannelState ch)
    {
        Texture2D tex = iconTextures[channelIndex];
        Color[] pixels = new Color[iconWidth * iconHeight];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;

        Color traceColor = channelTraceColors[channelIndex % channelTraceColors.Length];
        if (!ch.outputOn) traceColor *= 0.35f;

        float phaseOffset = ch.phaseDeg / 360f;
        int previousY = -1;
        for (int x = 0; x < iconWidth; x++)
        {
            float phase = (float)x / iconWidth + phaseOffset;
            float value = WaveformMath.Evaluate(ch.waveform, phase); // -1..1
            int y = Mathf.Clamp(Mathf.RoundToInt((value * 0.5f + 0.5f) * (iconHeight - 1)), 0, iconHeight - 1);

            PlotPoint(pixels, x, y, traceColor);

            if (previousY >= 0 && Mathf.Abs(y - previousY) > 1)
            {
                int step = y > previousY ? 1 : -1;
                for (int fillY = previousY; fillY != y; fillY += step)
                    PlotPoint(pixels, x, fillY, traceColor);
            }
            previousY = y;
        }

        tex.SetPixels(pixels);
        tex.Apply();
    }

    void PlotPoint(Color[] pixels, int x, int y, Color color)
    {
        int flippedY = iconHeight - 1 - y;
        int index = flippedY * iconWidth + x;
        if (index >= 0 && index < pixels.Length)
            pixels[index] = color;
    }
}
