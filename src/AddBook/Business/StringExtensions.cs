using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AddBook.Business
{
    internal static class StringExtensions
    {
        /// <summary>
        /// white space, em-dash, en-dash, underscore
        /// </summary>
        private static readonly Regex WordDelimiters = new Regex(@"[\s—–_]", RegexOptions.Compiled);

        /// <summary>
        /// characters that are not valid
        /// </summary>
        private static readonly Regex InvalidChars = new Regex(@"[^a-z0-9\-]", RegexOptions.Compiled);

        /// <summary>
        /// multiple hyphens
        /// </summary>
        private static readonly Regex MultipleHyphens = new Regex(@"-{2,}", RegexOptions.Compiled);

        private static string RemoveDiacritics(string stIn)
        {
            var stFormD = stIn.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in stFormD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        internal static string Slugify(this string value)
        {
            value = value.ToLowerInvariant(); // convert to lower case
            value = RemoveDiacritics(value); // remove diacritics (accents)
            value = WordDelimiters.Replace(value, "-"); // ensure all word delimiters are hyphens
            value = InvalidChars.Replace(value, ""); // strip out invalid characters
            value = MultipleHyphens.Replace(value, "-"); // replace multiple hyphens (-) with a single hyphen
            return value.Trim('-'); // trim hyphens (-) from ends
        }
    }
}