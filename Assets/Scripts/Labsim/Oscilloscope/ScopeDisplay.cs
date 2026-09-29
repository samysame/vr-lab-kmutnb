using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LabSim
{
    /// <summary>
    /// World-space Canvas placed on the DHO814 screen: waveform area (RawImage + WaveformPlotter),
    /// channel scales, timebase, trigger, run state and automatic measurements.
    /// </summary>
    public class ScopeDisplay : MonoBehaviour
    {
        public OscilloscopeController scope;
        [Tooltip("Child object holding the whole screen UI; hidden when the scope is off.")]
        public GameObject screenRoot;

        [Header("Waveform area")]
        public RawImage traceImage;
        public int textureWidth = 800;
        public int textureHeight = 640;
        [Range(1, 4)] public int traceThickness = 2;
        public Color32 background = new Color32(5, 8, 12, 255);
        public Color32 gridColor = new Color32(60, 60, 60, 255);
        public Color32 axisColor = new Color32(110, 110, 110, 255);
        public Color32 triggerColor = new Color32(255, 140, 0, 255);

        [Header("Labels")]
        public TMP_Text[] channelLabels = new TMP_Text[ScopeSpecs.ChannelCount];
        public TMP_Text timebaseLabel;
        public TMP_Text triggerLabel;
        public TMP_Text runStateLabel;
        public TMP_Text measurementsText;

        WaveformPlotter plotter;

        void Start()
        {
            if (scope == null) scope = GetComponentInParent<OscilloscopeController>();
            if (scope == null || traceImage == null)
            {
                Debug.LogError("[ScopeDisplay] Assign the OscilloscopeController and the trace RawImage.");
                enabled = false;
                return;
            }

            plotter = new WaveformPlotter(textureWidth, textureHeight, background);
            traceImage.texture = plotter.Texture;

            scope.OnAcquired += Redraw;
            scope.OnSettingsChanged += RefreshLabels;
            scope.Machine.OnPowerChanged += HandlePower;
            HandlePower(scope.IsPowered);
        }

        void OnDestroy()
        {
            if (scope != null)
            {
                scope.OnAcquired -= Redraw;
                scope.OnSettingsChanged -= RefreshLabels;
                if (scope.Machine != null) scope.Machine.OnPowerChanged -= HandlePower;
            }
            if (plotter != null) plotter.Dispose();
        }

        void HandlePower(bool on)
        {
            if (screenRoot != null) screenRoot.SetActive(on);
            if (!on) return;
            plotter.Clear();
            plotter.DrawGrid(ScopeSpecs.HorizontalDivisions, ScopeSpecs.VerticalDivisions, gridColor, axisColor);
            plotter.Apply();
            RefreshLabels();
        }

        void Redraw()
        {
            AcquisitionResult r = scope.LastResult;
            plotter.Clear();
            plotter.DrawGrid(ScopeSpecs.HorizontalDivisions, ScopeSpecs.VerticalDivisions, gridColor, axisColor);

            float vDiv = ScopeSpecs.VerticalDivisions;
            for (int c = 0; c < scope.channels.Length; c++)
            {
                ScopeChannel ch = scope.channels[c];
                if (!ch.enabled) continue;
                Color32 col = ScopeSpecs.ChannelColors[c];
                plotter.DrawLeftMarker((float)((ch.positionDiv + vDiv / 2f) / vDiv), col);
                if (r.channelValid[c]) plotter.DrawTrace(r.screenY[c], r.sampleCount, col, traceThickness);
            }

            // Trigger level (right marker, dashed line for 1.5 s after a change) and trigger position (top marker).
            ScopeChannel src = scope.channels[scope.trigger.sourceChannel];
            float yT = (float)((scope.trigger.level / src.voltsPerDiv + src.positionDiv + vDiv / 2f) / vDiv);
            plotter.DrawRightMarker(yT, triggerColor);
            if (Time.time - scope.LastTriggerLevelChange < 1.5f) plotter.DrawHorizontalLine(yT, triggerColor, true);

            double window = scope.timebase.Window;
            float xT = (float)((window * 0.5 - scope.timebase.delaySeconds) / window);
            plotter.DrawTopMarker(xT, triggerColor);

            plotter.Apply();
            RefreshLabels();
        }

        void RefreshLabels()
        {
            if (scope == null || !scope.IsPowered) return;

            for (int c = 0; c < channelLabels.Length && c < scope.channels.Length; c++)
            {
                if (channelLabels[c] == null) continue;
                ScopeChannel ch = scope.channels[c];
                string head = (c == scope.SelectedChannel ? "<u>" : "") + "CH" + (c + 1) + (c == scope.SelectedChannel ? "</u>" : "");
                channelLabels[c].text = ch.enabled
                    ? "<color=" + ScopeSpecs.ColorHex(c) + ">" + head + "  " + EngineeringFormat.Format(ch.voltsPerDiv, "V", 3) +
                      "/div  " + ch.coupling + "  " + ch.probeRatio.ToString("0") + "X" + (ch.invert ? "  INV" : "") + "</color>"
                    : "<color=#555555>" + head + " off</color>";
            }

            if (timebaseLabel != null)
                timebaseLabel.text = "H " + EngineeringFormat.Format(scope.timebase.secondsPerDiv, "s", 3) + "/div   D " +
                                     EngineeringFormat.Format(scope.timebase.delaySeconds, "s", 3);

            if (triggerLabel != null)
                triggerLabel.text = "T CH" + (scope.trigger.sourceChannel + 1) + " " +
                                    (scope.trigger.slope == TriggerSlope.Rising ? "montant" : "descendant") + "  " +
                                    EngineeringFormat.Format(scope.trigger.level, "V", 3) + "  " + scope.trigger.mode;

            if (runStateLabel != null) runStateLabel.text = RunStateText();

            if (measurementsText != null) measurementsText.text = BuildMeasurements();
        }

        string RunStateText()
        {
            if (!string.IsNullOrEmpty(scope.StatusMessage)) return "<color=#FFCC00>" + scope.StatusMessage + "</color>";
            switch (scope.RunState)
            {
                case ScopeRunState.Stopped: return "<color=#FF4040>STOP</color>";
                case ScopeRunState.SingleArmed: return "<color=#FFCC00>WAIT</color>";
                default:
                    return scope.LastResult != null && scope.LastResult.triggered
                        ? "<color=#3CFF6A>T'D</color>"
                        : "<color=#FFCC00>AUTO</color>";
            }
        }

        string BuildMeasurements()
        {
            StringBuilder sb = new StringBuilder();
            for (int c = 0; c < scope.channels.Length; c++)
            {
                if (!scope.channels[c].enabled) continue;
                Measurements m = scope.ChannelMeasurements[c];
                sb.Append("<color=").Append(ScopeSpecs.ColorHex(c)).Append(">CH").Append(c + 1).Append("</color>  ");
                if (!m.valid) { sb.AppendLine("----"); continue; }
                sb.Append("Vpp ").Append(EngineeringFormat.Format(m.vpp, "V", 3));
                sb.Append("   Vrms ").Append(EngineeringFormat.Format(m.vrms, "V", 3));
                sb.Append("   Moy ").Append(EngineeringFormat.Format(m.vavg, "V", 3));
                sb.Append("   f ").Append(EngineeringFormat.Format(m.frequency, "Hz", 4));
                if (!double.IsNaN(m.dutyPercent)) sb.Append("   RC ").Append(m.dutyPercent.ToString("F1")).Append(" %");
                if (m.clipped) sb.Append("  <color=#FF4040>SATURÉ</color>");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
