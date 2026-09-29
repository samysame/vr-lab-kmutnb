using System;

namespace LabSim
{
    /// <summary>
    /// Simulated acquisition of the DHO814:
    ///  1. sample the trigger source over a search window located in the past,
    ///  2. find the latest edge crossing the trigger level (with hysteresis), refined by bisection,
    ///  3. record every enabled channel around that instant (trigger point at screen center - delay),
    ///  4. apply probe ratio, coupling, noise and 12-bit quantization over 10 divisions.
    /// If no edge is found: Auto mode free-runs (unstable trace), Normal/Single keep the old record.
    /// </summary>
    public class AcquisitionEngine
    {
        public int triggerSearchSamples = 4000;
        public double searchWindowScreens = 2.0;
        /// <summary>RMS noise added at the input, in volts (before the probe ratio).</summary>
        public double inputNoiseVrms = 0.001;

        double[] searchBuffer = new double[0];
        double[] captureBuffer = new double[0];

        public bool Acquire(ScopeChannel[] channels, Timebase tb, TriggerSettings trig, double now,
                            bool allowFreeRun, AcquisitionResult r)
        {
            int n = r.sampleCount;
            double window = tb.Window;
            double dt = window / (n - 1);
            double triggerOffset = window * 0.5 - tb.delaySeconds;      // first sample -> trigger point
            double latestTrigger = now - (window - triggerOffset);      // whole record must be in the past
            double searchSpan = window * searchWindowScreens;

            double tTrig;
            bool found = FindTrigger(channels[trig.sourceChannel], trig, latestTrigger - searchSpan, searchSpan, out tTrig);
            if (!found)
            {
                if (!allowFreeRun) return false;
                tTrig = latestTrigger; // free run: arbitrary phase each frame
            }

            r.triggered = found;
            r.dt = dt;
            r.triggerTime = tTrig;
            r.firstSampleTime = tTrig - triggerOffset;

            for (int c = 0; c < channels.Length; c++)
            {
                if (!channels[c].enabled)
                {
                    r.channelValid[c] = false;
                    continue;
                }
                Capture(channels[c], r, c);
                r.channelValid[c] = true;
            }
            return true;
        }

        /// <summary>Voltage as displayed by the scope (probe ratio and invert applied, coupling not applied).</summary>
        public double DisplayVoltage(ScopeChannel ch, double t, bool addNoise)
        {
            if (ch.input == null || ch.coupling == Coupling.GND) return 0.0;
            double v = ch.input.ReadVoltage(t);
            if (addNoise && inputNoiseVrms > 0) v += WaveformMath.Gaussian() * inputNoiseVrms;
            v *= ch.probeRatio;
            return ch.invert ? -v : v;
        }

        /// <summary>Raw sampling without noise nor quantization (used by AutoScale).</summary>
        public void SampleRaw(ScopeChannel ch, double t0, double dt, double[] buffer, int count)
        {
            for (int i = 0; i < count; i++) buffer[i] = DisplayVoltage(ch, t0 + i * dt, false);
        }

        bool FindTrigger(ScopeChannel src, TriggerSettings trig, double start, double span, out double tTrig)
        {
            tTrig = 0;
            if (src == null || src.input == null) return false;

            int m = triggerSearchSamples;
            if (searchBuffer.Length != m) searchBuffer = new double[m];
            double step = span / (m - 1);

            double mean = 0;
            for (int i = 0; i < m; i++)
            {
                searchBuffer[i] = DisplayVoltage(src, start + i * step, true);
                mean += searchBuffer[i];
            }
            mean /= m;
            double acOffset = src.coupling == Coupling.AC ? mean : 0.0;

            double level = trig.level;
            double hyst = Math.Max(1e-9, trig.hysteresisDiv * src.voltsPerDiv);
            bool rising = trig.slope == TriggerSlope.Rising;
            bool armed = false;
            int foundIndex = -1;

            for (int i = 0; i < m; i++)
            {
                double v = searchBuffer[i] - acOffset;
                if (rising)
                {
                    if (v < level - hyst) armed = true;
                    else if (armed && v >= level) { foundIndex = i; armed = false; }
                }
                else
                {
                    if (v > level + hyst) armed = true;
                    else if (armed && v <= level) { foundIndex = i; armed = false; }
                }
            }
            if (foundIndex <= 0) return false;

            // Bisection on the noiseless signal between the two bracketing samples: sub-sample stability.
            double a = start + (foundIndex - 1) * step;
            double b = start + foundIndex * step;
            for (int k = 0; k < 30; k++)
            {
                double mid = 0.5 * (a + b);
                double v = DisplayVoltage(src, mid, false) - acOffset;
                bool beyond = rising ? v >= level : v <= level;
                if (beyond) b = mid; else a = mid;
            }
            tTrig = b;
            return true;
        }

        void Capture(ScopeChannel ch, AcquisitionResult r, int c)
        {
            int n = r.sampleCount;
            if (captureBuffer.Length != n) captureBuffer = new double[n];

            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                double v = DisplayVoltage(ch, r.firstSampleTime + i * r.dt, true);
                captureBuffer[i] = v;
                sum += v;
            }
            double acOffset = ch.coupling == Coupling.AC ? sum / n : 0.0;

            int maxCode = (1 << ScopeSpecs.AdcBits) - 1;
            double full = ScopeSpecs.AdcRangeDivisions;
            double screen = ScopeSpecs.VerticalDivisions;
            bool clipped = false;

            for (int i = 0; i < n; i++)
            {
                double div = (captureBuffer[i] - acOffset) / ch.voltsPerDiv + ch.positionDiv;
                double code = Math.Round((div + full / 2.0) / full * maxCode);
                if (code < 0) { code = 0; clipped = true; }
                else if (code > maxCode) { code = maxCode; clipped = true; }

                double divQ = code / maxCode * full - full / 2.0;
                r.screenY[c][i] = (float)((divQ + screen / 2.0) / screen);
                r.volts[c][i] = (float)((divQ - ch.positionDiv) * ch.voltsPerDiv);
            }
            r.clipped[c] = clipped;
        }
    }
}
