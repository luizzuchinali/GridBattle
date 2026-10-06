using System.Text;

namespace GridBattle.UI.Overlays
{
    /// <summary>Roman numerals for talent ranks ("II/III"): the same in every language.</summary>
    public static class RomanNumerals
    {
        private static readonly int[] Values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };

        private static readonly string[] Symbols =
            { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

        /// <summary>The numeral of <paramref name="number"/>; values below 1 come back as the plain number.</summary>
        public static string ToRoman(int number)
        {
            if (number < 1 || number > 3999) return number.ToString();

            var builder = new StringBuilder();
            for (var i = 0; i < Values.Length; i++)
            {
                while (number >= Values[i])
                {
                    builder.Append(Symbols[i]);
                    number -= Values[i];
                }
            }

            return builder.ToString();
        }

        /// <summary>"II/III": the rank the talent would have after taking it, out of its maximum.</summary>
        public static string Rank(int rank, int maxRank) => ToRoman(rank) + "/" + ToRoman(maxRank);
    }
}
