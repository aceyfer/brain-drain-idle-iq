using System.Globalization;
using System.Text;

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
                if (value > 0d && value < 1d)
                    return value.ToString("0.##", CultureInfo.InvariantCulture);
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

        /// <summary>
        /// Appends a formatted value into an existing StringBuilder without allocating a new string.
        /// Clears the builder before writing.
        /// </summary>
        public static void FormatInto(StringBuilder sb, double value)
        {
            sb.Clear();

            if (value < 0d)
            {
                sb.Append('-');
                AppendPositiveFormatted(sb, -value);
                return;
            }

            AppendPositiveFormatted(sb, value);
        }

        /// <summary>
        /// Appends a formatted value into an existing StringBuilder without clearing it first.
        /// </summary>
        public static void AppendFormatted(StringBuilder sb, double value)
        {
            if (value < 0d)
            {
                sb.Append('-');
                AppendPositiveFormatted(sb, -value);
                return;
            }

            AppendPositiveFormatted(sb, value);
        }

        private static void AppendPositiveFormatted(StringBuilder sb, double value)
        {
            if (value < 1000d)
            {
                if (value > 0d && value < 1d)
                    sb.Append(value.ToString("0.##", CultureInfo.InvariantCulture));
                else
                    sb.Append((long)value);
                return;
            }

            int suffixIndex = 0;
            double scaled = value;

            while (scaled >= 1000d && suffixIndex < Suffixes.Length - 1)
            {
                scaled /= 1000d;
                suffixIndex++;
            }

            sb.Append(scaled.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(Suffixes[suffixIndex]);
        }
    }
}
