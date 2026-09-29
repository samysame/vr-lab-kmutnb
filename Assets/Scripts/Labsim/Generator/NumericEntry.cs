using System.Globalization;
using System.Text;

namespace LabSim
{
    /// <summary>Keypad input buffer: digits, decimal point, sign; committed with a unit multiplier.</summary>
    public class NumericEntry
    {
        readonly StringBuilder buffer = new StringBuilder();
        bool negative;

        public bool IsActive { get { return buffer.Length > 0; } }
        public string Text { get { return (negative ? "-" : "") + buffer.ToString(); } }

        public void AppendDigit(int d)
        {
            if (buffer.Length < 12) buffer.Append((char)('0' + d));
        }

        public void AppendDecimalPoint()
        {
            if (buffer.ToString().IndexOf('.') >= 0) return;
            if (buffer.Length == 0) buffer.Append('0');
            buffer.Append('.');
        }

        public void ToggleSign()
        {
            negative = !negative;
            if (buffer.Length == 0) buffer.Append('0');
        }

        public void Backspace()
        {
            if (buffer.Length > 0) buffer.Length--;
            if (buffer.Length == 0) negative = false;
        }

        public void Clear()
        {
            buffer.Length = 0;
            negative = false;
        }

        /// <summary>Parses the buffer times 'multiplier' (1e3 for k, 1e-3 for m...). Clears on success.</summary>
        public bool TryCommit(double multiplier, out double value)
        {
            value = 0;
            if (buffer.Length == 0) return false;
            double v;
            if (!double.TryParse(buffer.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            {
                Clear();
                return false;
            }
            value = (negative ? -v : v) * multiplier;
            Clear();
            return true;
        }
    }
}
