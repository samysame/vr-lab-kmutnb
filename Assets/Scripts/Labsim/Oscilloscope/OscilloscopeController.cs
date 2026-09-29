using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabSim
{
    /// <summary>Every action a DHO814 key can trigger. A key whose buttonId equals the enum name works without binding.</summary>
    public enum ScopeAction
    {
        None,
        Channel1, Channel2, Channel3, Channel4,
        RunStop, Single, AutoScale, DefaultSetup,
        TriggerModeCycle, TriggerSlopeToggle, TriggerSourceNext, TriggerLevel50,
        CouplingCycle, ProbeCycle, InvertToggle,
        VoltsPerDivUp, VoltsPerDivDown, TimePerDivUp, TimePerDivDown,
        PositionUp, PositionDown, TriggerLevelUp, TriggerLevelDown
    }

    /// <summary>Function of a DHO814 knob. A knob whose knobId equals the enum name works without binding.</summary>
    public enum ScopeKnobFunction
    {
        None,
        VerticalScale,
        VerticalPosition,
        HorizontalScale,
        HorizontalPosition,
        TriggerLevel
    }

    [Serializable]
    public class ScopeBinding
    {
        public string controlId;
        public ScopeAction action;
        public ScopeBinding(string id, ScopeAction a) { controlId = id; action = a; }
    }

    [Serializable]
    public class ScopeKnobBinding
    {
        public string knobId;
        public ScopeKnobFunction function;
        public ScopeKnobBinding(string id, ScopeKnobFunction f) { knobId = id; function = f; }
    }

    /// <summary>
    /// Logic of the Rigol DHO814. Put it on the oscilloscope root next to MachineState.
    /// Runs the acquisition at 'refreshRate', computes the measurements and raises OnAcquired.
    /// Vertical knobs act on the selected channel, like on the real instrument.
    /// </summary>
    [RequireComponent(typeof(MachineState))]
    public class OscilloscopeController : MonoBehaviour
    {
        [Header("Channels")]
        public ScopeChannel[] channels = CreateChannels();
        [Tooltip("BNC input connectors, index 0 = CH1 ... 3 = CH4.")]
        public Connector[] inputConnectors = new Connector[ScopeSpecs.ChannelCount];

        [Header("Horizontal / trigger")]
        public Timebase timebase = new Timebase();
        public TriggerSettings trigger = new TriggerSettings();

        [Header("Acquisition")]
        [Range(200, 4000)] public int samplesPerScreen = 1000;
        [Range(5f, 60f)] public float refreshRate = 30f;
        public double inputNoiseVrms = 0.001;

        [Header("Bindings (id -> action)")]
        public List<ScopeBinding> bindings = new List<ScopeBinding>();
        public List<ScopeKnobBinding> knobBindings = new List<ScopeKnobBinding>();

        public int SelectedChannel { get; private set; }
        public ScopeRunState RunState { get; private set; }
        public AcquisitionResult LastResult { get; private set; }
        public Measurements[] ChannelMeasurements { get; private set; }
        /// <summary>Seconds of continuous successful triggering (used to validate scenarios).</summary>
        public float TriggerStableTime { get; private set; }
        /// <summary>Time.time of the last trigger level change (display shows a dashed level line).</summary>
        public float LastTriggerLevelChange { get; private set; }
        public string StatusMessage { get; private set; }
        public bool IsPowered { get { return machine != null && machine.IsPoweredOn; } }
        public MachineState Machine { get { return machine; } }
        public ScopeChannel Selected { get { return channels[SelectedChannel]; } }

        public event Action OnSettingsChanged;
        public event Action OnAcquired;

        MachineState machine;
        AcquisitionEngine engine;
        float acquisitionTimer;
        readonly Dictionary<string, ScopeAction> actionById = new Dictionary<string, ScopeAction>();
        readonly Dictionary<string, ScopeKnobFunction> knobById = new Dictionary<string, ScopeKnobFunction>();
        readonly Dictionary<ScopeAction, LitButton> litByAction = new Dictionary<ScopeAction, LitButton>();

        static ScopeChannel[] CreateChannels()
        {
            ScopeChannel[] arr = new ScopeChannel[ScopeSpecs.ChannelCount];
            for (int i = 0; i < arr.Length; i++) arr[i] = new ScopeChannel();
            arr[0].enabled = true;
            return arr;
        }

        void Awake()
        {
            machine = GetComponent<MachineState>();
            if (channels == null || channels.Length != ScopeSpecs.ChannelCount) channels = CreateChannels();

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].index = i;
                Connector input = i < inputConnectors.Length ? inputConnectors[i] : null;
                channels[i].input = input;
                if (input != null)
                {
                    input.role = ConnectorRole.Input;
                    input.Sink = channels[i];
                }
            }

            engine = new AcquisitionEngine();
            LastResult = new AcquisitionResult(ScopeSpecs.ChannelCount, samplesPerScreen);
            ChannelMeasurements = new Measurements[ScopeSpecs.ChannelCount];
            for (int i = 0; i < ChannelMeasurements.Length; i++) ChannelMeasurements[i] = Measurements.Invalid();
            RunState = ScopeRunState.Running;

            foreach (ScopeBinding b in bindings)
                if (b != null && !string.IsNullOrEmpty(b.controlId)) actionById[b.controlId] = b.action;
            foreach (ScopeKnobBinding k in knobBindings)
                if (k != null && !string.IsNullOrEmpty(k.knobId)) knobById[k.knobId] = k.function;

            HookControls();
            machine.OnPowerChanged += HandlePowerChanged;
        }

        void OnDestroy()
        {
            if (machine != null) machine.OnPowerChanged -= HandlePowerChanged;
        }

        // ------------------------------------------------------------------ acquisition loop

        void Update()
        {
            if (!IsPowered) return;

            acquisitionTimer += Time.deltaTime;
            if (acquisitionTimer < 1f / refreshRate) return;
            float elapsed = acquisitionTimer;
            acquisitionTimer = 0f;

            if (RunState == ScopeRunState.Stopped) return;

            engine.inputNoiseVrms = inputNoiseVrms;
            bool freeRun = trigger.mode == TriggerMode.Auto && RunState == ScopeRunState.Running;
            bool updated = engine.Acquire(channels, timebase, trigger, Time.timeAsDouble, freeRun, LastResult);

            if (updated && LastResult.triggered) TriggerStableTime += elapsed;
            else TriggerStableTime = 0f;
            if (!updated) return;

            for (int c = 0; c < channels.Length; c++)
            {
                ChannelMeasurements[c] = LastResult.channelValid[c]
                    ? MeasurementEngine.Measure(LastResult.volts[c], LastResult.sampleCount, LastResult.dt, LastResult.clipped[c],
                        // Ignore timing measurements on pure noise (< 0.2 div or < 5 mV)
                        Math.Max(0.005, 0.2 * channels[c].voltsPerDiv))
                    : Measurements.Invalid();
            }

            if (RunState == ScopeRunState.SingleArmed && LastResult.triggered)
            {
                RunState = ScopeRunState.Stopped;
                RefreshLights();
                if (OnSettingsChanged != null) OnSettingsChanged();
            }
            if (OnAcquired != null) OnAcquired();
        }

        // ------------------------------------------------------------------ controls

        void HookControls()
        {
            foreach (SimpleButton b in GetComponentsInChildren<SimpleButton>(true))
                b.OnPressed += pressed => Execute(pressed.buttonId);

            foreach (LitButton lit in GetComponentsInChildren<LitButton>(true))
            {
                if (lit is PowerButton) continue;
                lit.toggleOnClick = false;
                ScopeAction a = ResolveAction(lit.buttonId);
                if (a != ScopeAction.None) litByAction[a] = lit;
                lit.OnClicked += clicked => Execute(clicked.buttonId);
            }

            foreach (RotaryKnob k in GetComponentsInChildren<RotaryKnob>(true))
            {
                k.OnTurned += (knob, steps) => ApplyKnob(ResolveKnob(knob.knobId), steps);
                k.OnPushed += knob => PushKnob(ResolveKnob(knob.knobId));
            }
        }

        ScopeAction ResolveAction(string id)
        {
            ScopeAction a;
            if (!string.IsNullOrEmpty(id) && actionById.TryGetValue(id, out a)) return a;
            try { if (!string.IsNullOrEmpty(id)) return (ScopeAction)Enum.Parse(typeof(ScopeAction), id, true); }
            catch (ArgumentException) { }
            return ScopeAction.None;
        }

        ScopeKnobFunction ResolveKnob(string id)
        {
            ScopeKnobFunction f;
            if (!string.IsNullOrEmpty(id) && knobById.TryGetValue(id, out f)) return f;
            try { if (!string.IsNullOrEmpty(id)) return (ScopeKnobFunction)Enum.Parse(typeof(ScopeKnobFunction), id, true); }
            catch (ArgumentException) { }
            Debug.Log("[DHO814] Unmapped knob '" + id + "'.");
            return ScopeKnobFunction.None;
        }

        public void Execute(string controlId)
        {
            ScopeAction a = ResolveAction(controlId);
            if (a == ScopeAction.None) { Debug.Log("[DHO814] Unmapped key '" + controlId + "'."); return; }
            Execute(a);
        }

        public void Execute(ScopeAction a)
        {
            if (!IsPowered) return;
            StatusMessage = null;
            ScopeChannel sel = Selected;

            switch (a)
            {
                case ScopeAction.Channel1: ChannelKey(0); break;
                case ScopeAction.Channel2: ChannelKey(1); break;
                case ScopeAction.Channel3: ChannelKey(2); break;
                case ScopeAction.Channel4: ChannelKey(3); break;

                case ScopeAction.RunStop:
                    RunState = RunState == ScopeRunState.Running ? ScopeRunState.Stopped : ScopeRunState.Running;
                    break;
                case ScopeAction.Single: RunState = ScopeRunState.SingleArmed; break;
                case ScopeAction.AutoScale: AutoScale(); break;
                case ScopeAction.DefaultSetup: DefaultSetup(); break;

                case ScopeAction.TriggerModeCycle:
                    trigger.mode = (TriggerMode)(((int)trigger.mode + 1) % 3);
                    if (trigger.mode == TriggerMode.Single) RunState = ScopeRunState.SingleArmed;
                    else if (RunState == ScopeRunState.SingleArmed) RunState = ScopeRunState.Running;
                    break;
                case ScopeAction.TriggerSlopeToggle:
                    SetTriggerSlope(trigger.slope == TriggerSlope.Rising ? TriggerSlope.Falling : TriggerSlope.Rising);
                    break;
                case ScopeAction.TriggerSourceNext: SetTriggerSource((trigger.sourceChannel + 1) % channels.Length); break;
                case ScopeAction.TriggerLevel50: TriggerLevelTo50(); break;

                case ScopeAction.CouplingCycle: sel.coupling = (Coupling)(((int)sel.coupling + 1) % 3); break;
                case ScopeAction.ProbeCycle:
                    sel.probeRatio = sel.probeRatio >= 100 ? 1 : sel.probeRatio * 10;
                    break;
                case ScopeAction.InvertToggle: sel.invert = !sel.invert; break;

                case ScopeAction.VoltsPerDivUp: ApplyKnob(ScopeKnobFunction.VerticalScale, -1); break;
                case ScopeAction.VoltsPerDivDown: ApplyKnob(ScopeKnobFunction.VerticalScale, +1); break;
                case ScopeAction.TimePerDivUp: ApplyKnob(ScopeKnobFunction.HorizontalScale, -1); break;
                case ScopeAction.TimePerDivDown: ApplyKnob(ScopeKnobFunction.HorizontalScale, +1); break;
                case ScopeAction.PositionUp: ApplyKnob(ScopeKnobFunction.VerticalPosition, +1); break;
                case ScopeAction.PositionDown: ApplyKnob(ScopeKnobFunction.VerticalPosition, -1); break;
                case ScopeAction.TriggerLevelUp: ApplyKnob(ScopeKnobFunction.TriggerLevel, +1); break;
                case ScopeAction.TriggerLevelDown: ApplyKnob(ScopeKnobFunction.TriggerLevel, -1); break;
            }
            NotifySettings();
        }

        /// <summary>Knob steps: clockwise (+) = finer scale / move up / move right.</summary>
        public void ApplyKnob(ScopeKnobFunction f, int steps)
        {
            if (!IsPowered || f == ScopeKnobFunction.None) return;
            ScopeChannel sel = Selected;
            switch (f)
            {
                case ScopeKnobFunction.VerticalScale:
                    SetVoltsPerDiv(SelectedChannel, ScaleSequence.Step(sel.voltsPerDiv, -steps,
                        ScopeSpecs.MinVoltsPerDiv, ScopeSpecs.MaxVoltsPerDiv));
                    break;
                case ScopeKnobFunction.VerticalPosition:
                    sel.positionDiv = Math.Max(-10, Math.Min(10, Math.Round((sel.positionDiv + 0.1 * steps) * 10) / 10));
                    break;
                case ScopeKnobFunction.HorizontalScale:
                    SetTimePerDiv(ScaleSequence.Step(timebase.secondsPerDiv, -steps,
                        ScopeSpecs.MinTimePerDiv, ScopeSpecs.MaxTimePerDiv));
                    break;
                case ScopeKnobFunction.HorizontalPosition:
                    double lim = timebase.Window;
                    timebase.delaySeconds = Math.Max(-lim, Math.Min(lim, timebase.delaySeconds + 0.2 * steps * timebase.secondsPerDiv));
                    break;
                case ScopeKnobFunction.TriggerLevel:
                    ScopeChannel src = channels[trigger.sourceChannel];
                    SetTriggerLevel(trigger.level + 0.1 * steps * src.voltsPerDiv);
                    break;
            }
            NotifySettings();
        }

        public void PushKnob(ScopeKnobFunction f)
        {
            if (!IsPowered) return;
            switch (f)
            {
                case ScopeKnobFunction.VerticalPosition: Selected.positionDiv = 0; break;
                case ScopeKnobFunction.HorizontalPosition: timebase.delaySeconds = 0; break;
                case ScopeKnobFunction.TriggerLevel: TriggerLevelTo50(); break;
            }
            NotifySettings();
        }

        void ChannelKey(int i)
        {
            ScopeChannel ch = channels[i];
            if (!ch.enabled) { ch.enabled = true; SelectedChannel = i; }
            else if (SelectedChannel == i)
            {
                ch.enabled = false;
                for (int k = 1; k <= channels.Length; k++)
                {
                    int j = (i + k) % channels.Length;
                    if (channels[j].enabled) { SelectedChannel = j; break; }
                }
            }
            else SelectedChannel = i;
        }

        // ------------------------------------------------------------------ public API (UI, scenarios)

        public void SetChannelEnabled(int i, bool on)
        {
            channels[i].enabled = on;
            if (on) SelectedChannel = i;
            NotifySettings();
        }

        public void SetVoltsPerDiv(int i, double v)
        {
            channels[i].voltsPerDiv = Math.Max(ScopeSpecs.MinVoltsPerDiv, Math.Min(ScopeSpecs.MaxVoltsPerDiv, v));
            NotifySettings();
        }

        public void SetTimePerDiv(double s)
        {
            timebase.secondsPerDiv = Math.Max(ScopeSpecs.MinTimePerDiv, Math.Min(ScopeSpecs.MaxTimePerDiv, s));
            NotifySettings();
        }

        public void SetTriggerLevel(double volts)
        {
            // The level is kept within +/- 5 divisions around the screen center.
            ScopeChannel src = channels[trigger.sourceChannel];
            double center = -src.positionDiv * src.voltsPerDiv;
            double half = 5.0 * src.voltsPerDiv;
            trigger.level = Math.Max(center - half, Math.Min(center + half, volts));
            LastTriggerLevelChange = Time.time;
            NotifySettings();
        }

        public void SetTriggerSlope(TriggerSlope s) { trigger.slope = s; NotifySettings(); }
        public void SetTriggerMode(TriggerMode m) { trigger.mode = m; NotifySettings(); }
        public void SetTriggerSource(int ch) { trigger.sourceChannel = Mathf.Clamp(ch, 0, channels.Length - 1); NotifySettings(); }

        public void TriggerLevelTo50()
        {
            Measurements m = ChannelMeasurements[trigger.sourceChannel];
            if (m.valid) SetTriggerLevel(0.5 * (m.vmax + m.vmin));
        }

        public void DefaultSetup()
        {
            for (int i = 0; i < channels.Length; i++)
            {
                ScopeChannel c = channels[i];
                c.enabled = i == 0;
                c.voltsPerDiv = 1.0;
                c.positionDiv = 0;
                c.coupling = Coupling.DC;
                c.probeRatio = 1.0;
                c.invert = false;
            }
            timebase.secondsPerDiv = 500e-6;
            timebase.delaySeconds = 0;
            trigger.sourceChannel = 0;
            trigger.level = 0;
            trigger.slope = TriggerSlope.Rising;
            trigger.mode = TriggerMode.Auto;
            SelectedChannel = 0;
            RunState = ScopeRunState.Running;
            NotifySettings();
        }

        /// <summary>
        /// AUTO key: for every connected and active input, estimates Vmin/Vmax and the period, then
        /// sets V/div so the signal spans about 6 divisions, s/div for 2 to 5 periods, and the trigger at 50 %.
        /// </summary>
        public void AutoScale()
        {
            if (!IsPowered) return;
            int triggerSource = -1;
            double period = double.NaN;

            for (int i = 0; i < channels.Length; i++)
            {
                ScopeChannel ch = channels[i];
                Connector peer = ch.input != null ? ch.input.Peer : null;
                if (peer == null || peer.Source == null || !peer.Source.IsOutputActive) continue;

                double vmin, vmax, p;
                EstimateSignal(ch, out vmin, out vmax, out p);
                ch.enabled = true;
                ch.coupling = Coupling.DC;
                ch.positionDiv = 0;
                double maxAbs = Math.Max(Math.Abs(vmin), Math.Abs(vmax));
                double vpp = vmax - vmin;
                ch.voltsPerDiv = Math.Min(ScopeSpecs.MaxVoltsPerDiv,
                                 ScaleSequence.Ceil(Math.Max(1e-3, Math.Max(vpp / 6.0, maxAbs / 3.6))));

                if (triggerSource < 0 || (double.IsNaN(period) && !double.IsNaN(p)))
                {
                    triggerSource = i;
                    period = p;
                    trigger.level = 0.5 * (vmin + vmax);
                }
            }

            if (triggerSource < 0)
            {
                StatusMessage = "AUTO : aucun signal détecté";
                NotifySettings();
                return;
            }

            if (!double.IsNaN(period))
                timebase.secondsPerDiv = Math.Max(ScopeSpecs.MinTimePerDiv, ScaleSequence.Ceil(period / 5.0));
            timebase.delaySeconds = 0;
            trigger.sourceChannel = triggerSource;
            trigger.slope = TriggerSlope.Rising;
            trigger.mode = TriggerMode.Auto;
            SelectedChannel = triggerSource;
            RunState = ScopeRunState.Running;
            LastTriggerLevelChange = Time.time;
            NotifySettings();
        }

        /// <summary>
        /// Zoom-out search: starts with a 1 us window (no aliasing up to the generator limits) and
        /// enlarges it x10 until at least 3 rising edges are visible.
        /// </summary>
        void EstimateSignal(ScopeChannel ch, out double vmin, out double vmax, out double period)
        {
            const int n = 20000;
            double[] buf = new double[n];
            double span = 1e-6;
            vmin = vmax = 0;
            period = double.NaN;

            for (int iter = 0; iter < 16; iter++)
            {
                double dt = span / (n - 1);
                engine.SampleRaw(ch, Time.timeAsDouble - span, dt, buf, n);
                vmin = double.MaxValue;
                vmax = double.MinValue;
                for (int i = 0; i < n; i++) { if (buf[i] < vmin) vmin = buf[i]; if (buf[i] > vmax) vmax = buf[i]; }
                double vpp = vmax - vmin;

                if (vpp < 1e-6)
                {
                    if (span >= 1.0) return; // DC signal
                    span *= 10.0;
                    continue;
                }

                double mid = 0.5 * (vmax + vmin), hyst = 0.1 * vpp;
                int count = 0;
                double first = -1, last = -1;
                bool armed = false;
                for (int i = 1; i < n; i++)
                {
                    if (buf[i] < mid - hyst) armed = true;
                    else if (armed && buf[i] >= mid)
                    {
                        armed = false;
                        if (first < 0) first = i;
                        last = i;
                        count++;
                    }
                }

                if (count >= 3 && count <= 400)
                {
                    period = (last - first) / (count - 1) * dt;
                    return;
                }
                if (count > 400) return;   // noise-like signal: no period
                span *= 10.0;
                if (span > 100.0) return;
            }
        }

        void HandlePowerChanged(bool on)
        {
            TriggerStableTime = 0;
            if (on)
            {
                RunState = ScopeRunState.Running;
                RefreshLights();
            }
            NotifySettings();
        }

        void NotifySettings()
        {
            RefreshLights();
            if (OnSettingsChanged != null) OnSettingsChanged();
        }

        void RefreshLights()
        {
            if (!IsPowered) return;
            SetLight(ScopeAction.Channel1, channels[0].enabled);
            SetLight(ScopeAction.Channel2, channels[1].enabled);
            SetLight(ScopeAction.Channel3, channels[2].enabled);
            SetLight(ScopeAction.Channel4, channels[3].enabled);
            SetLight(ScopeAction.RunStop, RunState != ScopeRunState.Stopped);
            SetLight(ScopeAction.Single, RunState == ScopeRunState.SingleArmed);
        }

        void SetLight(ScopeAction a, bool on)
        {
            LitButton lit;
            if (litByAction.TryGetValue(a, out lit) && lit != null) lit.SetStateWithoutNotify(on);
        }

        /// <summary>Right-click the component header > Reset to fill both lists with default ids.</summary>
        void Reset()
        {
            bindings = new List<ScopeBinding>();
            foreach (ScopeAction a in Enum.GetValues(typeof(ScopeAction)))
                if (a != ScopeAction.None) bindings.Add(new ScopeBinding(a.ToString(), a));
            knobBindings = new List<ScopeKnobBinding>();
            foreach (ScopeKnobFunction f in Enum.GetValues(typeof(ScopeKnobFunction)))
                if (f != ScopeKnobFunction.None) knobBindings.Add(new ScopeKnobBinding(f.ToString(), f));
        }
    }
}
