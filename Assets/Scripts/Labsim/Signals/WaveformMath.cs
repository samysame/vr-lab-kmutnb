using System;

namespace LabSim
{
    /// <summary>
    /// Pure math for waveform synthesis and theoretical values.
    /// v(t) = offset + (Vpp / 2) * s(phi), phi = frac(f * t + phase / 360), s in [-1, 1].
    /// </summary>
    public static class WaveformMath
    {
        [ThreadStatic] static Random rng;

        /// <summary>Instantaneous voltage at time t (seconds).</summary>
        public static double Evaluate(WaveformParameters p, double t)
        {
            double a = 0.5 * p.amplitude;
            switch (p.type)
            {
                case WaveformType.DC:
                    return p.offset;
                case WaveformType.Noise:
                    // Gaussian noise with sigma = Vpp / 6 (about 99.7 % of samples inside Vpp), clipped to +/- Vpp/2.
                    double g = Gaussian() / 3.0;
                    return p.offset + a * Math.Max(-1.0, Math.Min(1.0, g));
            }
            double phase = Frac(p.frequency * t + p.phaseDeg / 360.0);
            return p.offset + a * Normalized(p, phase);
        }

        /// <summary>Normalized shape s(phase) in [-1, 1], phase in [0, 1).</summary>
        public static double Normalized(WaveformParameters p, double phase)
        {
            switch (p.type)
            {
                case WaveformType.Sine:
                    return Math.Sin(2.0 * Math.PI * phase);

                case WaveformType.Square:
                case WaveformType.Pulse:
                    return phase < p.dutyCyclePercent / 100.0 ? 1.0 : -1.0;

                case WaveformType.Ramp:
                    double s = p.symmetryPercent / 100.0;
                    if (s <= 0.0) return 1.0 - 2.0 * phase;
                    if (s >= 1.0) return -1.0 + 2.0 * phase;
                    return phase < s
                        ? -1.0 + 2.0 * phase / s
                        : 1.0 - 2.0 * (phase - s) / (1.0 - s);

                default:
                    return 0.0;
            }
        }

        /// <summary>Theoretical mean value (V).</summary>
        public static double TheoreticalMean(WaveformParameters p)
        {
            double a = 0.5 * p.amplitude;
            switch (p.type)
            {
                case WaveformType.Square:
                case WaveformType.Pulse:
                    return p.offset + a * (2.0 * p.dutyCyclePercent / 100.0 - 1.0);
                default:
                    return p.offset; // sine, ramp, noise, DC
            }
        }

        /// <summary>Theoretical true RMS value (V), offset included.</summary>
        public static double TheoreticalRms(WaveformParameters p)
        {
            double a = 0.5 * p.amplitude;
            double o = p.offset;
            switch (p.type)
            {
                case WaveformType.Sine:
                    return Math.Sqrt(o * o + a * a / 2.0);
                case WaveformType.Square:
                case WaveformType.Pulse:
                    double d = p.dutyCyclePercent / 100.0;
                    return Math.Sqrt(d * (o + a) * (o + a) + (1.0 - d) * (o - a) * (o - a));
                case WaveformType.Ramp:
                    return Math.Sqrt(o * o + a * a / 3.0);
                case WaveformType.Noise:
                    return Math.Sqrt(o * o + (a / 3.0) * (a / 3.0));
                default:
                    return Math.Abs(o);
            }
        }

        public static double Frac(double x)
        {
            return x - Math.Floor(x);
        }

        /// <summary>Standard normal random value (Box-Muller).</summary>
        public static double Gaussian()
        {
            if (rng == null) rng = new Random();
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
