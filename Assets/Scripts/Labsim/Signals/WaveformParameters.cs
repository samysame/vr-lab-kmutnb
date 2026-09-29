using System;

namespace LabSim
{
    /// <summary>All settings describing a periodic waveform, in SI units.</summary>
    [Serializable]
    public class WaveformParameters
    {
        public WaveformType type = WaveformType.Sine;

        /// <summary>Hz.</summary>
        public double frequency = 1000.0;

        /// <summary>Peak-to-peak amplitude in volts, as displayed by the generator (i.e. into the configured load).</summary>
        public double amplitude = 5.0;

        /// <summary>DC offset in volts.</summary>
        public double offset = 0.0;

        /// <summary>Start phase in degrees.</summary>
        public double phaseDeg = 0.0;

        /// <summary>High time / period for Square and Pulse, in percent.</summary>
        public double dutyCyclePercent = 50.0;

        /// <summary>Ramp symmetry in percent: 100 = rising sawtooth, 50 = triangle, 0 = falling sawtooth.</summary>
        public double symmetryPercent = 100.0;

        public double Period { get { return frequency > 0 ? 1.0 / frequency : double.PositiveInfinity; } }

        public WaveformParameters Clone()
        {
            return (WaveformParameters)MemberwiseClone();
        }
    }
}
