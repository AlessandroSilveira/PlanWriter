using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PlanWriter.Application.Common
{
    public static class Slugify
    {
        public static string From(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim().ToLowerInvariant();
            s = RemoveDiacritics(s);
            s = Regex.Replace(s, @"[^a-z0-9\s-]", "");       // remove símbolos
            s = Regex.Replace(s, @"\s+", "-");               // espaços → hífen
            s = Regex.Replace(s, @"-+", "-").Trim('-');      // hífens em excesso
            return s;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in from ch in normalized let uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) 
                     where uc != System.Globalization.UnicodeCategory.NonSpacingMark select ch)
            {
                sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
