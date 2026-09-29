using System;

namespace LabSim
{
    /// <summary>
    /// One output channel of the generator, seen as a Thevenin source (Vopen, 50 ohms).
    /// The displayed amplitude assumes the configured load, like on the real instrument:
    ///   High-Z setting : Vopen = Vset
    ///   50 ohm setting : Vopen = Vset * (RL + Rout) / RL = 2 * Vset
    /// </summary>
    [Serializable]
    public class GeneratorChannel : ISignalSource
    {
        public WaveformParameters wave = new WaveformParameters();
        public OutputLoad load = OutputLoad.HighZ;

        [NonSerialized] public bool outputEnabled;
        [NonSerialized] public int index;
        [NonSerialized] Func<bool> isPowered;

        public void Bind(int channelIndex, Func<bool> poweredQuery)
        {
            index = channelIndex;
            isPowered = poweredQuery;
        }

        public bool IsOutputActive
        {
            get { return outputEnabled && (isPowered == null || isPowered()); }
        }

        public double OutputImpedance { get { return GeneratorSpecs.OutputImpedance; } }

        public string SourceName { get { return "DG822 CH" + (index + 1); } }

        public double SampleOpenCircuit(double t)
        {
            double vSet = WaveformMath.Evaluate(wave, t);
            if (load == OutputLoad.HighZ) return vSet;
            const double rl = 50.0;
            return vSet * (rl + OutputImpedance) / rl;
        }
    }
}
