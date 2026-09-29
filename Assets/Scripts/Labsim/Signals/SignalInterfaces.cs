namespace LabSim
{
    /// <summary>Anything that drives a connector (generator channel...). Thevenin model: Vopen(t) + Rout.</summary>
    public interface ISignalSource
    {
        bool IsOutputActive { get; }
        double OutputImpedance { get; }
        double SampleOpenCircuit(double t);
        string SourceName { get; }
    }

    /// <summary>Anything that loads a connector (oscilloscope input...).</summary>
    public interface ISignalSink
    {
        double InputImpedance { get; }
        string SinkName { get; }
    }

    public static class SignalMath
    {
        /// <summary>Voltage divider between source impedance Rs and load RL.</summary>
        public static double Divider(double vOpen, double rSource, double rLoad)
        {
            if (double.IsPositiveInfinity(rLoad)) return vOpen;
            return vOpen * rLoad / (rSource + rLoad);
        }
    }
}
