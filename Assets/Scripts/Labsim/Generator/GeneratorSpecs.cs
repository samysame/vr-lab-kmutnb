namespace LabSim
{
    public enum OutputLoad
    {
        HighZ,
        FiftyOhm
    }

    public enum GeneratorParameter
    {
        Frequency,
        Amplitude,
        Offset,
        Phase,
        DutyCycle,
        Symmetry
    }

    /// <summary>
    /// Limits of the Rigol DG822 Pro used for validation.
    /// NOTE: approximate values for the prototype, to be checked against the official datasheet.
    /// </summary>
    public static class GeneratorSpecs
    {
        public const int ChannelCount = 2;
        public const double OutputImpedance = 50.0;       // ohms
        public const double MinFrequency = 1e-6;          // Hz
        public const double MinAmplitude = 1e-3;          // Vpp
        public const double MaxVoltageHighZ = 10.0;       // |offset| + Vpp/2 into High-Z
        public const double MaxVoltage50Ohm = 5.0;        // |offset| + Vpp/2 into 50 ohms

        public static double MaxFrequency(WaveformType type)
        {
            switch (type)
            {
                case WaveformType.Sine: return 25e6;
                case WaveformType.Square: return 10e6;
                case WaveformType.Pulse: return 10e6;
                case WaveformType.Ramp: return 1e6;
                default: return 25e6;
            }
        }

        public static double MaxVoltage(OutputLoad load)
        {
            return load == OutputLoad.HighZ ? MaxVoltageHighZ : MaxVoltage50Ohm;
        }

        public static string Unit(GeneratorParameter p)
        {
            switch (p)
            {
                case GeneratorParameter.Frequency: return "Hz";
                case GeneratorParameter.Amplitude: return "Vpp";
                case GeneratorParameter.Offset: return "V";
                case GeneratorParameter.Phase: return "°";
                default: return "%";
            }
        }

        public static string Label(GeneratorParameter p)
        {
            switch (p)
            {
                case GeneratorParameter.Frequency: return "Fréq";
                case GeneratorParameter.Amplitude: return "Ampl";
                case GeneratorParameter.Offset: return "Offset";
                case GeneratorParameter.Phase: return "Phase";
                case GeneratorParameter.DutyCycle: return "Rapport cycl.";
                default: return "Symétrie";
            }
        }

        public static string WaveName(WaveformType t)
        {
            switch (t)
            {
                case WaveformType.Sine: return "Sinus";
                case WaveformType.Square: return "Carré";
                case WaveformType.Ramp: return "Rampe";
                case WaveformType.Pulse: return "Impulsion";
                case WaveformType.Noise: return "Bruit";
                default: return "DC";
            }
        }

        /// <summary>Default edited digit (power of ten of the step) when a parameter is selected.</summary>
        public static int DefaultCursorDecade(GeneratorParameter p)
        {
            switch (p)
            {
                case GeneratorParameter.Frequency: return 2;   // 100 Hz steps
                case GeneratorParameter.Amplitude: return -1;  // 0.1 V
                case GeneratorParameter.Offset: return -1;     // 0.1 V
                case GeneratorParameter.Phase: return 1;       // 10 deg
                default: return 0;                             // 1 %
            }
        }

        public static void CursorRange(GeneratorParameter p, out int min, out int max)
        {
            switch (p)
            {
                case GeneratorParameter.Frequency: min = -6; max = 7; break;
                case GeneratorParameter.Amplitude: min = -3; max = 1; break;
                case GeneratorParameter.Offset: min = -3; max = 0; break;
                case GeneratorParameter.Phase: min = -1; max = 2; break;
                default: min = -1; max = 1; break;
            }
        }
    }
}
