using System.Globalization;
using System.Text;
using TMPro;

namespace BrainDrain.Core
{
    /// <summary>
    /// Zero-allocation HUD text helpers using a shared StringBuilder and NumberFormatter.FormatInto.
    /// </summary>
    public static class HUDNumericFormatter
    {
        private static readonly StringBuilder SharedBuilder = new StringBuilder(64);

        public static void SetBrainPowerCounter(TextMeshProUGUI label, double brainPower)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            NumberFormatter.FormatInto(SharedBuilder, brainPower);
            SharedBuilder.Append(" BRAIN POWER");
            label.SetText(SharedBuilder);
        }

        public static void SetCapacity(TextMeshProUGUI label, double brainPower)
        {
            if (label == null)
            {
                return;
            }

            double percent = System.Math.Min(100.0, (brainPower / 500000.0) * 100.0);
            SharedBuilder.Clear();
            SharedBuilder.Append(percent.ToString("F1", CultureInfo.InvariantCulture));
            SharedBuilder.Append("% ABSORBED");
            label.SetText(SharedBuilder);
        }

        public static void SetCumulativeBrainPower(TextMeshProUGUI label, double cumulativeBrainPower)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            SharedBuilder.Append("LIFETIME: ");
            NumberFormatter.FormatInto(SharedBuilder, cumulativeBrainPower);
            label.SetText(SharedBuilder);
        }

        /// <summary>Muted grey used for the "DRY" daily-cap-throttled tag -- deliberately distinct from OVERCHARGED's orange, since this signals a reduction, not a boost.</summary>
        private const string ThrottledTagColorHex = "#888888";

        public static void SetBpps(TextMeshProUGUI label, double idleBpps, bool throttled)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            NumberFormatter.FormatInto(SharedBuilder, idleBpps);
            SharedBuilder.Append(" BPPS");
            if (throttled)
            {
                SharedBuilder.Append(" <color=").Append(ThrottledTagColorHex).Append(">DRY</color>");
            }
            label.SetText(SharedBuilder);
        }

        public static void SetCash(TextMeshProUGUI label, double currentCash, double cashPerSecond, bool throttled)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            SharedBuilder.Append('$');
            NumberFormatter.AppendFormatted(SharedBuilder, currentCash);
            SharedBuilder.Append(" ($");
            NumberFormatter.AppendFormatted(SharedBuilder, cashPerSecond);
            SharedBuilder.Append("/s)");
            if (throttled)
            {
                SharedBuilder.Append(" <color=").Append(ThrottledTagColorHex).Append(">DRY</color>");
            }
            label.SetText(SharedBuilder);
        }

        public static void SetPoints(TextMeshProUGUI label, double currentPoints, bool isRebirthActivated)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            NumberFormatter.FormatInto(SharedBuilder, currentPoints);
            SharedBuilder.Append(isRebirthActivated ? " POINTS" : " RESTORATION PTS");
            label.SetText(SharedBuilder);
            label.color = isRebirthActivated ? UnityEngine.Color.white : new UnityEngine.Color(1f, 0.85f, 0.2f, 1f);
        }

        public static void SetRank(TextMeshProUGUI label, string rankName)
        {
            if (label == null || string.IsNullOrEmpty(rankName))
            {
                return;
            }

            SharedBuilder.Clear();
            SharedBuilder.Append(rankName.ToUpperInvariant());
            label.SetText(SharedBuilder);
        }

        public static void SetRebirthCount(TextMeshProUGUI label, int rebirthCount)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            SharedBuilder.Append("SNOTTINGS: ");
            SharedBuilder.Append(rebirthCount);
            label.SetText(SharedBuilder);
        }

        public static void SetIllumisnottiTitle(TextMeshProUGUI label, string title)
        {
            if (label == null)
            {
                return;
            }

            SharedBuilder.Clear();
            if (!string.IsNullOrEmpty(title))
            {
                SharedBuilder.Append(title.ToUpperInvariant());
            }

            label.SetText(SharedBuilder);
        }
    }
}
