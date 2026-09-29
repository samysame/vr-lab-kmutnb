using System;
using System.Collections.Generic;

namespace LabSim
{
    /// <summary>Automatic measurements of one channel (NaN = not measurable).</summary>
    public struct Measurements
    {
        public bool valid;
        public bool clipped;
        public double vmax, vmin, vpp, vavg, vrms;
        public double frequency, period, dutyPercent, riseTime;

        public static Measurements Invalid()
        {
            Measurements m = new Measurements();
            m.vmax = m.vmin = m.vpp = m.vavg = m.vrms = double.NaN;
            m.frequency = m.period = m.dutyPercent = m.riseTime = double.NaN;
            return m;
        }
    }

    /// <summary>
    /// Measurement algorithms working on the displayed record (like the real instrument):
    ///  - Vmax, Vmin, Vpp = Vmax - Vmin
    ///  - edges detected on the mid level (Vmax + Vmin) / 2 with 10 % hysteresis, linear interpolation
    ///  - period = mean distance between rising edges, f = 1 / T
    ///  - duty = high time / period
    ///  - Vavg and Vrms computed over an integer number of periods when possible
    ///  - rise time between 10 % and 90 % of Vpp
    /// </summary>
    public static class MeasurementEngine
    {
        [ThreadStatic] static List<double> rising;
        [ThreadStatic] static List<double> falling;

        /// <param name="minPeriodicVpp">
        /// Below this peak-to-peak amplitude the record is treated as noise:
        /// amplitude values are still reported but no frequency / duty / rise time.
        /// </param>
        public static Measurements Measure(float[] v, int n, double dt, bool clipped, double minPeriodicVpp = 0.0)
        {
            Measurements m = Measurements.Invalid();
            if (v == null || n < 4) return m;
            if (rising == null) { rising = new List<double>(); falling = new List<double>(); }

            double max = double.MinValue, min = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (v[i] > max) max = v[i];
                if (v[i] < min) min = v[i];
            }
            m.valid = true;
            m.clipped = clipped;
            m.vmax = max;
            m.vmin = min;
            m.vpp = max - min;

            double mid = 0.5 * (max + min);
            double hyst = 0.1 * m.vpp;
            bool periodic = m.vpp > Math.Max(1e-6, minPeriodicVpp);
            if (periodic) FindEdges(v, n, mid, hyst, rising, falling);
            else { rising.Clear(); falling.Clear(); }

            int from = 0, to = n - 1;
            if (periodic && rising.Count >= 2)
            {
                double first = rising[0], last = rising[rising.Count - 1];
                double periodSamples = (last - first) / (rising.Count - 1);
                m.period = periodSamples * dt;
                m.frequency = 1.0 / m.period;

                double highSum = 0;
                int highCount = 0;
                for (int k = 0; k < rising.Count - 1; k++)
                {
                    double f = FirstBetween(falling, rising[k], rising[k + 1]);
                    if (!double.IsNaN(f)) { highSum += f - rising[k]; highCount++; }
                }
                if (highCount > 0) m.dutyPercent = 100.0 * (highSum / highCount) / periodSamples;

                from = (int)Math.Ceiling(first);
                to = (int)Math.Floor(last);
            }

            double sum = 0, sumSq = 0;
            int count = 0;
            for (int i = from; i <= to && i < n; i++)
            {
                sum += v[i];
                sumSq += (double)v[i] * v[i];
                count++;
            }
            if (count > 0)
            {
                m.vavg = sum / count;
                m.vrms = Math.Sqrt(sumSq / count);
            }

            if (periodic) m.riseTime = RiseTime(v, n, dt, min + 0.1 * m.vpp, min + 0.9 * m.vpp);
            return m;
        }

        /// <summary>
        /// Phase of 'other' relative to 'reference' in degrees, in (-180, 180].
        /// Positive = other leads. Requires at least two rising edges on the reference.
        /// </summary>
        public static double PhaseDegrees(float[] reference, float[] other, int n)
        {
            List<double> rRef = new List<double>(), fRef = new List<double>();
            List<double> rOth = new List<double>(), fOth = new List<double>();
            double midR, hystR, midO, hystO;
            if (!MidLevel(reference, n, out midR, out hystR) || !MidLevel(other, n, out midO, out hystO)) return double.NaN;
            FindEdges(reference, n, midR, hystR, rRef, fRef);
            FindEdges(other, n, midO, hystO, rOth, fOth);
            if (rRef.Count < 2 || rOth.Count < 1) return double.NaN;

            double period = (rRef[rRef.Count - 1] - rRef[0]) / (rRef.Count - 1);
            double tRef = rRef[0];
            double best = double.NaN;
            foreach (double t in rOth)
                if (double.IsNaN(best) || Math.Abs(t - tRef) < Math.Abs(best - tRef)) best = t;

            double phase = -360.0 * (best - tRef) / period;
            while (phase <= -180.0) phase += 360.0;
            while (phase > 180.0) phase -= 360.0;
            return phase;
        }

        static bool MidLevel(float[] v, int n, out double mid, out double hyst)
        {
            double max = double.MinValue, min = double.MaxValue;
            for (int i = 0; i < n; i++) { if (v[i] > max) max = v[i]; if (v[i] < min) min = v[i]; }
            mid = 0.5 * (max + min);
            hyst = 0.1 * (max - min);
            return max - min > 1e-6;
        }

        /// <summary>Edge positions in fractional sample indices, detected on 'level' with hysteresis.</summary>
        static void FindEdges(float[] v, int n, double level, double hyst, List<double> rise, List<double> fall)
        {
            rise.Clear();
            fall.Clear();
            bool armedRise = false, armedFall = false;
            for (int i = 1; i < n; i++)
            {
                double x = v[i];
                if (x < level - hyst) armedRise = true;
                if (x > level + hyst) armedFall = true;

                if (armedRise && x >= level && v[i - 1] < level)
                {
                    rise.Add(Interpolate(v[i - 1], x, level, i));
                    armedRise = false;
                }
                if (armedFall && x <= level && v[i - 1] > level)
                {
                    fall.Add(Interpolate(v[i - 1], x, level, i));
                    armedFall = false;
                }
            }
        }

        static double Interpolate(double a, double b, double level, int i)
        {
            double d = b - a;
            double frac = Math.Abs(d) < 1e-12 ? 1.0 : (level - a) / d;
            return (i - 1) + frac;
        }

        static double FirstBetween(List<double> list, double a, double b)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] > a && list[i] < b) return list[i];
            return double.NaN;
        }

        static double RiseTime(float[] v, int n, double dt, double lo, double hi)
        {
            bool below = false;
            double tLo = double.NaN;
            for (int i = 1; i < n; i++)
            {
                if (v[i] < lo) { below = true; tLo = double.NaN; continue; }
                if (below && double.IsNaN(tLo) && v[i - 1] < lo && v[i] >= lo) tLo = Interpolate(v[i - 1], v[i], lo, i);
                if (!double.IsNaN(tLo) && v[i - 1] < hi && v[i] >= hi)
                    return (Interpolate(v[i - 1], v[i], hi, i) - tLo) * dt;
            }
            return double.NaN;
        }
    }
}
