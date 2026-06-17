using System.Globalization;

namespace BrainDrain.Core
{
    /// <summary>
    /// Formats large idle-game currency values into compact, readable strings.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = { string.Empty, "K", "M", "B", "T", "Qa", "Qi" };

        /// <summary>
        /// Converts a numeric value to a compact idle string (e.g. 1250000 -> 1.25M).
        /// Values under 1000 show no decimals.
        /// </summary>
        public static string Format(double value)
        {
            if (value < 0d)
            {
                return "-" + Format(-value);
            }

            if (value < 1000d)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }

            int suffixIndex = 0;
            double scaled = value;

            while (scaled >= 1000d && suffixIndex < Suffixes.Length - 1)
            {
                scaled /= 1000d;
                suffixIndex++;
            }

            return scaled.ToString("0.00", CultureInfo.InvariantCulture) + Suffixes[suffixIndex];
        }
    }
}
