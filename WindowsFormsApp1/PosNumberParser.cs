using System;
using System.Globalization;

namespace WindowsFormsApp1
{
    public static class PosNumberParser
    {
        private static readonly CultureInfo[] AcceptedCultures =
        {
            CultureInfo.InvariantCulture,
            CultureInfo.CurrentCulture,
            new CultureInfo("en-US"),
            new CultureInfo("si-LK")
        };

        public static bool TryParseMoney(string input, out decimal value, bool allowZero = true)
        {
            return TryParsePositiveDecimal(input, out value, allowZero);
        }

        public static bool TryParseQuantity(string input, out decimal value, bool allowZero = false)
        {
            return TryParsePositiveDecimal(input, out value, allowZero);
        }

        public static decimal ParseRequiredMoney(string input, string fieldName, bool allowZero = true)
        {
            decimal value;
            if (!TryParseMoney(input, out value, allowZero))
            {
                throw new InvalidOperationException(fieldName + " must be a valid " + (allowZero ? "non-negative" : "positive") + " amount.");
            }

            return value;
        }

        public static decimal ParseRequiredQuantity(string input, string fieldName, bool allowZero = false)
        {
            decimal value;
            if (!TryParseQuantity(input, out value, allowZero))
            {
                throw new InvalidOperationException(fieldName + " must be a valid " + (allowZero ? "non-negative" : "positive") + " quantity.");
            }

            return value;
        }

        private static bool TryParsePositiveDecimal(string input, out decimal value, bool allowZero)
        {
            value = 0m;
            string text = (input ?? string.Empty).Trim();
            if (text.Length == 0 ||
                text.IndexOf("not available", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("nan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("infinity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;
            foreach (CultureInfo culture in AcceptedCultures)
            {
                decimal parsed;
                if (decimal.TryParse(text, styles, culture, out parsed))
                {
                    if (parsed < 0m || (!allowZero && parsed == 0m))
                    {
                        return false;
                    }

                    value = parsed;
                    return true;
                }
            }

            return false;
        }
    }
}
