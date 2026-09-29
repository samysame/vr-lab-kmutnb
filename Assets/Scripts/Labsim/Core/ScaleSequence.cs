using System;

namespace LabSim
{
    /// <summary>
    /// 1-2-5 sequence used by oscilloscopes for V/div and s/div
    /// (..., 100m, 200m, 500m, 1, 2, 5, 10, ...).
    /// </summary>
    public static class ScaleSequence
    {
        static readonly double[] Mantissas = { 1, 2, 5 };

        /// <summary>Index of the nearest 1-2-5 value (index = 3 * exponent + mantissaIndex).</summary>
        public static int ToIndex(double v)
        {
            if (v <= 0) v = 1e-12;
            int e = (int)Math.Floor(Math.Log10(v));
            double m = v / Math.Pow(10, e);
            int mi = m < 1.5 ? 0 : (m < 3.5 ? 1 : (m < 7.5 ? 2 : 3));
            if (mi == 3) { mi = 0; e++; }
            return e * 3 + mi;
        }

        public static double FromIndex(int index)
        {
            int e = (int)Math.Floor(index / 3.0);
            int mi = index - e * 3;
            return Mantissas[mi] * Math.Pow(10, e);
        }

        /// <summary>Moves by 'steps' positions in the sequence, clamped to [min, max].</summary>
        public static double Step(double current, int steps, double min, double max)
        {
            double v = FromIndex(ToIndex(current) + steps);
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>Smallest 1-2-5 value greater than or equal to v.</summary>
        public static double Ceil(double v)
        {
            int i = ToIndex(v);
            double c = FromIndex(i);
            while (c < v * (1 - 1e-9)) { i++; c = FromIndex(i); }
            return c;
        }
    }
}
