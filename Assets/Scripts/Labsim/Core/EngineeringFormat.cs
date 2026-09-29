using System;
using System.Globalization;

namespace LabSim
{
    /// <summary>Formats physical values with SI prefixes, e.g. 1.000 kHz, 500.0 mV.</summary>
    public static class EngineeringFormat
    {
        static readonly string[] Prefixes = { "p", "n", "µ", "m", "", "k", "M", "G" };
        const int PrefixOffset = 4; // index of the empty prefix

        public static string Format(double value, string unit, int significantDigits = 4)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "----" + Suffix("", unit);
            if (value == 0) return (0.0).ToString("F" + Math.Max(0, significantDigits - 1), CultureInfo.InvariantCulture) + Suffix("", unit);

            double abs = Math.Abs(value);
            int exp3 = (int)Math.Floor(Math.Log10(abs) / 3.0);
            exp3 = Math.Max(-PrefixOffset, Math.Min(Prefixes.Length - 1 - PrefixOffset, exp3));

            for (int pass = 0; pass < 2; pass++)
            {
                double scaled = value / Math.Pow(10, exp3 * 3);
                double a = Math.Abs(scaled);
                int intDigits = a >= 100 ? 3 : (a >= 10 ? 2 : 1);
                int decimals = Math.Max(0, significantDigits - intDigits);
                string s = scaled.ToString("F" + decimals, CultureInfo.InvariantCulture);
                // Rounding can produce "1000.0": move to the next prefix once.
                if (Math.Abs(double.Parse(s, CultureInfo.InvariantCulture)) >= 1000 && exp3 < Prefixes.Length - 1 - PrefixOffset)
                {
                    exp3++;
                    continue;
                }
                return s + Suffix(Prefixes[exp3 + PrefixOffset], unit);
            }
            return value.ToString("G4", CultureInfo.InvariantCulture) + Suffix("", unit);
        }

        static string Suffix(string prefix, string unit)
        {
            return (prefix.Length + unit.Length) > 0 ? " " + prefix + unit : "";
        }
    }
}
