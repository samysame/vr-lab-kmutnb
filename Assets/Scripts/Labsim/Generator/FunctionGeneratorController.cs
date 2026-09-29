using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Logic of the Rigol DG822 Pro. Put it on the generator root, next to MachineState.
    /// At Awake it hooks every SimpleButton / LitButton / RotaryKnob found in its children and
    /// translates their ids into GeneratorAction through the bindings list (or the enum name).
    /// </summary>
    [RequireComponent(typeof(MachineState))]
    public class FunctionGeneratorController : MonoBehaviour
    {
        [Header("Channels")]
        public GeneratorChannel[] channels = { new GeneratorChannel(), new GeneratorChannel() };
        [Tooltip("BNC output connectors, index 0 = CH1, 1 = CH2.")]
        public Connector[] outputConnectors = new Connector[GeneratorSpecs.ChannelCount];

        [Header("Key bindings (buttonId -> action)")]
        [Tooltip("Optional. A key whose buttonId equals an action name (e.g. WaveSine) works without entry.")]
        public List<GeneratorBinding> bindings = new List<GeneratorBinding>();

        public int SelectedChannel { get; private set; }
        public GeneratorParameter SelectedParameter { get; private set; }
        public int CursorDecade { get; private set; }
        public NumericEntry Entry { get; private set; }
        public string StatusMessage { get; private set; }
        public bool IsPowered { get { return machine != null && machine.IsPoweredOn; } }
        public MachineState Machine { get { return machine; } }

        /// <summary>Raised after any change of settings (display refresh, scenarios).</summary>
        public event Action OnChanged;

        MachineState machine;
        readonly Dictionary<string, GeneratorAction> actionById = new Dictionary<string, GeneratorAction>();
        readonly Dictionary<GeneratorAction, LitButton> litByAction = new Dictionary<GeneratorAction, LitButton>();

        void Awake()
        {
            machine = GetComponent<MachineState>();
            Entry = new NumericEntry();
            EnsureChannels();

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].Bind(i, () => machine.IsPoweredOn);
                if (i < outputConnectors.Length && outputConnectors[i] != null)
                {
                    outputConnectors[i].role = ConnectorRole.Output;
                    outputConnectors[i].Source = channels[i];
                }
            }

            SelectParameter(GeneratorParameter.Frequency);
            BuildActionMap();
            HookControls();
            machine.OnPowerChanged += HandlePowerChanged;
        }

        void OnDestroy()
        {
            if (machine != null) machine.OnPowerChanged -= HandlePowerChanged;
        }

        void EnsureChannels()
        {
            if (channels == null || channels.Length != GeneratorSpecs.ChannelCount)
                channels = new GeneratorChannel[] { new GeneratorChannel(), new GeneratorChannel() };
            for (int i = 0; i < channels.Length; i++)
                if (channels[i] == null) channels[i] = new GeneratorChannel();
        }

        // ------------------------------------------------------------------ controls

        void BuildActionMap()
        {
            actionById.Clear();
            foreach (GeneratorBinding b in bindings)
                if (b != null && !string.IsNullOrEmpty(b.controlId)) actionById[b.controlId] = b.action;
        }

        GeneratorAction Resolve(string id)
        {
            GeneratorAction a;
            if (!string.IsNullOrEmpty(id) && actionById.TryGetValue(id, out a)) return a;
            try
            {
                if (!string.IsNullOrEmpty(id)) return (GeneratorAction)Enum.Parse(typeof(GeneratorAction), id, true);
            }
            catch (ArgumentException) { }
            return GeneratorAction.None;
        }

        void HookControls()
        {
            foreach (SimpleButton b in GetComponentsInChildren<SimpleButton>(true))
                b.OnPressed += pressed => Execute(pressed.buttonId);

            foreach (LitButton lit in GetComponentsInChildren<LitButton>(true))
            {
                if (lit is PowerButton) continue;
                lit.toggleOnClick = false; // the controller owns the light state
                GeneratorAction a = Resolve(lit.buttonId);
                if (a != GeneratorAction.None) litByAction[a] = lit;
                lit.OnClicked += clicked => Execute(clicked.buttonId);
            }

            foreach (RotaryKnob k in GetComponentsInChildren<RotaryKnob>(true))
            {
                k.OnTurned += (knob, steps) => AdjustSelected(steps);
                k.OnPushed += knob => MoveCursor(-1);
            }
        }

        public void Execute(string controlId)
        {
            GeneratorAction a = Resolve(controlId);
            if (a == GeneratorAction.None)
            {
                Debug.Log("[DG822] Unmapped key '" + controlId + "'.");
                return;
            }
            Execute(a);
        }

        public void Execute(GeneratorAction a)
        {
            if (!IsPowered) return;
            StatusMessage = null;

            switch (a)
            {
                case GeneratorAction.SelectChannel1: SelectChannel(0); break;
                case GeneratorAction.SelectChannel2: SelectChannel(1); break;
                case GeneratorAction.ToggleChannel: SelectChannel(1 - SelectedChannel); break;
                case GeneratorAction.ToggleOutput1: SetOutput(0, !channels[0].outputEnabled); break;
                case GeneratorAction.ToggleOutput2: SetOutput(1, !channels[1].outputEnabled); break;

                case GeneratorAction.WaveSine: SetWaveform(SelectedChannel, WaveformType.Sine); break;
                case GeneratorAction.WaveSquare: SetWaveform(SelectedChannel, WaveformType.Square); break;
                case GeneratorAction.WaveRamp: SetWaveform(SelectedChannel, WaveformType.Ramp); break;
                case GeneratorAction.WavePulse: SetWaveform(SelectedChannel, WaveformType.Pulse); break;
                case GeneratorAction.WaveNoise: SetWaveform(SelectedChannel, WaveformType.Noise); break;
                case GeneratorAction.WaveDC: SetWaveform(SelectedChannel, WaveformType.DC); break;

                case GeneratorAction.SelectFrequency: SelectParameter(GeneratorParameter.Frequency); break;
                case GeneratorAction.SelectAmplitude: SelectParameter(GeneratorParameter.Amplitude); break;
                case GeneratorAction.SelectOffset: SelectParameter(GeneratorParameter.Offset); break;
                case GeneratorAction.SelectPhase: SelectParameter(GeneratorParameter.Phase); break;
                case GeneratorAction.SelectDutyCycle: SelectParameter(GeneratorParameter.DutyCycle); break;
                case GeneratorAction.SelectSymmetry: SelectParameter(GeneratorParameter.Symmetry); break;
                case GeneratorAction.NextParameter: SelectParameter(NextRelevantParameter()); break;

                case GeneratorAction.CursorLeft: MoveCursor(+1); break;
                case GeneratorAction.CursorRight: MoveCursor(-1); break;
                case GeneratorAction.ValueUp: AdjustSelected(+1); break;
                case GeneratorAction.ValueDown: AdjustSelected(-1); break;

                case GeneratorAction.DecimalPoint: Entry.AppendDecimalPoint(); break;
                case GeneratorAction.Sign: Entry.ToggleSign(); break;
                case GeneratorAction.Backspace: Entry.Backspace(); break;
                case GeneratorAction.CancelEntry: Entry.Clear(); break;
                case GeneratorAction.EnterBase: CommitEntry(1.0); break;
                case GeneratorAction.EnterKilo: CommitEntry(1e3); break;
                case GeneratorAction.EnterMega: CommitEntry(1e6); break;
                case GeneratorAction.EnterMilli: CommitEntry(1e-3); break;
                case GeneratorAction.EnterMicro: CommitEntry(1e-6); break;

                case GeneratorAction.ToggleLoad:
                    SetLoad(SelectedChannel, channels[SelectedChannel].load == OutputLoad.HighZ ? OutputLoad.FiftyOhm : OutputLoad.HighZ);
                    break;
                case GeneratorAction.Preset: Preset(); break;

                default:
                    if (a >= GeneratorAction.Digit0 && a <= GeneratorAction.Digit9)
                        Entry.AppendDigit(a - GeneratorAction.Digit0);
                    break;
            }
            NotifyChanged();
        }

        // ------------------------------------------------------------------ public API (UI, scenarios)

        public GeneratorChannel Channel(int i) { return channels[i]; }

        public void SelectChannel(int i)
        {
            SelectedChannel = Mathf.Clamp(i, 0, channels.Length - 1);
            Entry.Clear();
            if (!IsRelevant(SelectedParameter)) SelectParameter(GeneratorParameter.Frequency);
            NotifyChanged();
        }

        public void SelectParameter(GeneratorParameter p)
        {
            SelectedParameter = p;
            CursorDecade = GeneratorSpecs.DefaultCursorDecade(p);
            Entry.Clear();
            NotifyChanged();
        }

        public void SetOutput(int ch, bool on)
        {
            channels[ch].outputEnabled = on && IsPowered;
            NotifyChanged();
        }

        public void SetLoad(int ch, OutputLoad load)
        {
            channels[ch].load = load;
            // Re-validate amplitude/offset against the new voltage limits.
            SetParameter(ch, GeneratorParameter.Amplitude, channels[ch].wave.amplitude);
            NotifyChanged();
        }

        public void SetWaveform(int ch, WaveformType type)
        {
            channels[ch].wave.type = type;
            SetParameter(ch, GeneratorParameter.Frequency, channels[ch].wave.frequency); // clamp to new max
            if (!IsRelevant(SelectedParameter)) SelectParameter(GeneratorParameter.Frequency);
            NotifyChanged();
        }

        public double GetParameter(int ch, GeneratorParameter p)
        {
            WaveformParameters w = channels[ch].wave;
            switch (p)
            {
                case GeneratorParameter.Frequency: return w.frequency;
                case GeneratorParameter.Amplitude: return w.amplitude;
                case GeneratorParameter.Offset: return w.offset;
                case GeneratorParameter.Phase: return w.phaseDeg;
                case GeneratorParameter.DutyCycle: return w.dutyCyclePercent;
                default: return w.symmetryPercent;
            }
        }

        /// <summary>Sets a parameter with instrument-like validation. Returns the applied value.</summary>
        public double SetParameter(int ch, GeneratorParameter p, double value)
        {
            GeneratorChannel c = channels[ch];
            WaveformParameters w = c.wave;
            double vMax = GeneratorSpecs.MaxVoltage(c.load);
            double requested = value;

            switch (p)
            {
                case GeneratorParameter.Frequency:
                    value = Clamp(value, GeneratorSpecs.MinFrequency, GeneratorSpecs.MaxFrequency(w.type));
                    w.frequency = value;
                    break;
                case GeneratorParameter.Amplitude:
                    value = Clamp(value, GeneratorSpecs.MinAmplitude, 2.0 * vMax);
                    w.amplitude = value;
                    w.offset = Clamp(w.offset, -(vMax - value / 2.0), vMax - value / 2.0);
                    break;
                case GeneratorParameter.Offset:
                    double lim = Math.Max(0.0, vMax - w.amplitude / 2.0);
                    value = Clamp(value, -lim, lim);
                    w.offset = value;
                    break;
                case GeneratorParameter.Phase:
                    value = WaveformMath.Frac(value / 360.0) * 360.0;
                    w.phaseDeg = value;
                    break;
                case GeneratorParameter.DutyCycle:
                    value = Clamp(value, 1.0, 99.0);
                    w.dutyCyclePercent = value;
                    break;
                case GeneratorParameter.Symmetry:
                    value = Clamp(value, 0.0, 100.0);
                    w.symmetryPercent = value;
                    break;
            }

            if (p != GeneratorParameter.Phase && Math.Abs(value - requested) > 1e-12 * Math.Max(1.0, Math.Abs(requested)))
                StatusMessage = "Valeur limitée à " + EngineeringFormat.Format(value, GeneratorSpecs.Unit(p), 5);

            NotifyChanged();
            return value;
        }

        /// <summary>Knob / arrow keys: adds steps * 10^cursor to the selected parameter.</summary>
        public void AdjustSelected(int steps)
        {
            if (!IsPowered) return;
            Entry.Clear();
            double current = GetParameter(SelectedChannel, SelectedParameter);
            double step = Math.Pow(10, CursorDecade);
            SetParameter(SelectedChannel, SelectedParameter, current + steps * step);
        }

        public void MoveCursor(int delta)
        {
            int min, max;
            GeneratorSpecs.CursorRange(SelectedParameter, out min, out max);
            CursorDecade = Mathf.Clamp(CursorDecade + delta, min, max);
            NotifyChanged();
        }

        /// <summary>Factory-like default state (outputs off).</summary>
        public void Preset()
        {
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].wave = new WaveformParameters();
                channels[i].load = OutputLoad.HighZ;
                channels[i].outputEnabled = false;
            }
            SelectedChannel = 0;
            SelectParameter(GeneratorParameter.Frequency);
            StatusMessage = null;
            NotifyChanged();
        }

        public bool IsRelevant(GeneratorParameter p)
        {
            WaveformType t = channels[SelectedChannel].wave.type;
            switch (p)
            {
                case GeneratorParameter.DutyCycle: return t == WaveformType.Square || t == WaveformType.Pulse;
                case GeneratorParameter.Symmetry: return t == WaveformType.Ramp;
                case GeneratorParameter.Frequency:
                case GeneratorParameter.Phase: return t != WaveformType.DC && t != WaveformType.Noise;
                case GeneratorParameter.Amplitude: return t != WaveformType.DC;
                default: return true;
            }
        }

        // ------------------------------------------------------------------ internals

        GeneratorParameter NextRelevantParameter()
        {
            int count = Enum.GetValues(typeof(GeneratorParameter)).Length;
            int p = (int)SelectedParameter;
            for (int i = 0; i < count; i++)
            {
                p = (p + 1) % count;
                if (IsRelevant((GeneratorParameter)p)) return (GeneratorParameter)p;
            }
            return GeneratorParameter.Frequency;
        }

        void CommitEntry(double multiplier)
        {
            double v;
            if (Entry.TryCommit(multiplier, out v)) SetParameter(SelectedChannel, SelectedParameter, v);
        }

        void HandlePowerChanged(bool on)
        {
            // Like the real instrument, outputs are always off after a power cycle.
            for (int i = 0; i < channels.Length; i++) channels[i].outputEnabled = false;
            Entry.Clear();
            NotifyChanged();
        }

        void NotifyChanged()
        {
            RefreshLights();
            if (OnChanged != null) OnChanged();
        }

        void RefreshLights()
        {
            if (!IsPowered) return; // LitButtons switch themselves off on power loss
            WaveformType t = channels[SelectedChannel].wave.type;
            SetLight(GeneratorAction.ToggleOutput1, channels[0].outputEnabled);
            SetLight(GeneratorAction.ToggleOutput2, channels[1].outputEnabled);
            SetLight(GeneratorAction.SelectChannel1, SelectedChannel == 0);
            SetLight(GeneratorAction.SelectChannel2, SelectedChannel == 1);
            SetLight(GeneratorAction.WaveSine, t == WaveformType.Sine);
            SetLight(GeneratorAction.WaveSquare, t == WaveformType.Square);
            SetLight(GeneratorAction.WaveRamp, t == WaveformType.Ramp);
            SetLight(GeneratorAction.WavePulse, t == WaveformType.Pulse);
            SetLight(GeneratorAction.WaveNoise, t == WaveformType.Noise);
            SetLight(GeneratorAction.WaveDC, t == WaveformType.DC);
        }

        void SetLight(GeneratorAction a, bool on)
        {
            LitButton lit;
            if (litByAction.TryGetValue(a, out lit) && lit != null) lit.SetStateWithoutNotify(on);
        }

        static double Clamp(double v, double min, double max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        /// <summary>Right-click the component header > Reset to fill the list with one entry per action.</summary>
        void Reset()
        {
            bindings = new List<GeneratorBinding>();
            foreach (GeneratorAction a in Enum.GetValues(typeof(GeneratorAction)))
                if (a != GeneratorAction.None) bindings.Add(new GeneratorBinding(a.ToString(), a));
        }
    }
}
