using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace FashionStore.Helpers;
public static class SlugHelper
{
    public static string ToSlug(this string text)
    {
        var s = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in s)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        var result = Regex.Replace(sb.ToString(), @"[^a-z0-9\s-]", "");
        return Regex.Replace(result, @"[\s-]+", "-").Trim('-');
    }
}