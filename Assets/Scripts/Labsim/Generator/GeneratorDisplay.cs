using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LabSim
{
    /// <summary>
    /// World-space Canvas placed on the DG822 Pro screen. Shows, for each channel, the waveform,
    /// its parameters (selected one highlighted), the output/load state and a small preview plot.
    /// </summary>
    public class GeneratorDisplay : MonoBehaviour
    {
        public FunctionGeneratorController generator;
        [Tooltip("Child object holding the whole screen UI; hidden when the generator is off.")]
        public GameObject screenRoot;

        [Header("Per-channel widgets (index 0 = CH1)")]
        public TMP_Text[] headerTexts = new TMP_Text[2];
        public TMP_Text[] parameterTexts = new TMP_Text[2];
        public RawImage[] previewImages = new RawImage[2];

        [Header("Status line (keypad entry, warnings)")]
        public TMP_Text statusText;

        [Header("Preview")]
        public int previewWidth = 256;
        public int previewHeight = 96;
        public Color32 previewBackground = new Color32(10, 12, 20, 255);
        public Color32 previewGrid = new Color32(50, 55, 70, 255);
        public Color32[] channelColors = { new Color32(255, 215, 0, 255), new Color32(0, 200, 255, 255) };

        const string Highlight = "#FF9A1F";
        WaveformPlotter[] plotters;
        float[] previewBuffer;

        void Start()
        {
            if (generator == null) generator = GetComponentInParent<FunctionGeneratorController>();
            if (generator == null)
            {
                Debug.LogError("[GeneratorDisplay] No FunctionGeneratorController assigned.");
                enabled = false;
                return;
            }

            plotters = new WaveformPlotter[previewImages.Length];
            previewBuffer = new float[previewWidth];
            for (int i = 0; i < previewImages.Length; i++)
            {
                if (previewImages[i] == null) continue;
                plotters[i] = new WaveformPlotter(previewWidth, previewHeight, previewBackground);
                previewImages[i].texture = plotters[i].Texture;
            }

            generator.OnChanged += Refresh;
            generator.Machine.OnPowerChanged += HandlePower;
            HandlePower(generator.IsPowered);
        }

        void OnDestroy()
        {
            if (generator != null)
            {
                generator.OnChanged -= Refresh;
                if (generator.Machine != null) generator.Machine.OnPowerChanged -= HandlePower;
            }
            if (plotters != null) foreach (WaveformPlotter p in plotters) if (p != null) p.Dispose();
        }

        void HandlePower(bool on)
        {
            if (screenRoot != null) screenRoot.SetActive(on);
            if (on) Refresh();
        }

        public void Refresh()
        {
            if (generator == null || !generator.IsPowered) return;

            for (int ch = 0; ch < generator.channels.Length; ch++)
            {
                GeneratorChannel c = generator.channels[ch];
                bool selected = ch == generator.SelectedChannel;

                if (ch < headerTexts.Length && headerTexts[ch] != null)
                {
                    headerTexts[ch].text = string.Format("{0}CH{1}  {2}   {3}   {4}",
                        selected ? "<color=" + Highlight + ">> </color>" : "  ",
                        ch + 1,
                        GeneratorSpecs.WaveName(c.wave.type),
                        c.outputEnabled ? "<color=#3CFF6A>ON</color>" : "<color=#888888>OFF</color>",
                        c.load == OutputLoad.HighZ ? "HighZ" : "50 Ohm");
                }

                if (ch < parameterTexts.Length && parameterTexts[ch] != null)
                    parameterTexts[ch].text = BuildParameterText(ch, selected);

                if (plotters != null && ch < plotters.Length && plotters[ch] != null)
                    DrawPreview(plotters[ch], c.wave, channelColors[Mathf.Min(ch, channelColors.Length - 1)]);
            }

            if (statusText != null)
            {
                if (generator.Entry.IsActive)
                    statusText.text = "Saisie : " + generator.Entry.Text + "_   (" +
                                      GeneratorSpecs.Label(generator.SelectedParameter) + ")";
                else if (!string.IsNullOrEmpty(generator.StatusMessage))
                    statusText.text = "<color=#FFCC00>" + generator.StatusMessage + "</color>";
                else
                    statusText.text = "Pas : " + EngineeringFormat.Format(System.Math.Pow(10, generator.CursorDecade),
                                      GeneratorSpecs.Unit(generator.SelectedParameter), 3);
            }
        }

        string BuildParameterText(int ch, bool channelSelected)
        {
            WaveformParameters w = generator.channels[ch].wave;
            StringBuilder sb = new StringBuilder();
            AppendLine(sb, ch, channelSelected, GeneratorParameter.Frequency, EngineeringFormat.Format(w.frequency, "Hz", 7));
            AppendLine(sb, ch, channelSelected, GeneratorParameter.Amplitude, EngineeringFormat.Format(w.amplitude, "Vpp", 4));
            AppendLine(sb, ch, channelSelected, GeneratorParameter.Offset, EngineeringFormat.Format(w.offset, "V", 4));
            AppendLine(sb, ch, channelSelected, GeneratorParameter.Phase, w.phaseDeg.ToString("F1") + " °");
            if (w.type == WaveformType.Square || w.type == WaveformType.Pulse)
                AppendLine(sb, ch, channelSelected, GeneratorParameter.DutyCycle, w.dutyCyclePercent.ToString("F1") + " %");
            if (w.type == WaveformType.Ramp)
                AppendLine(sb, ch, channelSelected, GeneratorParameter.Symmetry, w.symmetryPercent.ToString("F1") + " %");
            return sb.ToString();
        }

        void AppendLine(StringBuilder sb, int ch, bool channelSelected, GeneratorParameter p, string value)
        {
            bool hl = channelSelected && generator.SelectedParameter == p;
            string line = GeneratorSpecs.Label(p) + " : " + value;
            sb.AppendLine(hl ? "<color=" + Highlight + "><b>" + line + "</b></color>" : line);
        }

        /// <summary>Two periods of the waveform, auto-scaled to the preview height.</summary>
        void DrawPreview(WaveformPlotter plotter, WaveformParameters w, Color32 color)
        {
            plotter.Clear();
            plotter.DrawGrid(4, 2, previewGrid, previewGrid);

            double half = System.Math.Max(System.Math.Abs(w.offset) + w.amplitude / 2.0, 1e-3);
            int n = previewBuffer.Length;
            double duration = (w.type == WaveformType.DC || w.type == WaveformType.Noise || w.frequency <= 0)
                ? 1.0 : 2.0 / w.frequency;
            for (int i = 0; i < n; i++)
            {
                double t = duration * i / (n - 1);
                double v = WaveformMath.Evaluate(w, t);
                previewBuffer[i] = (float)(0.5 + 0.45 * v / half);
            }
            plotter.DrawTrace(previewBuffer, n, color, 2);
            plotter.Apply();
        }
    }
}
