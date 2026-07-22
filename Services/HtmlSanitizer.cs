using System.Text.RegularExpressions;

namespace ApparelShop.Services;

public static class HtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "a", "ul", "ol", "li",
        "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "code",
        "img", "table", "thead", "tbody", "tr", "th", "td", "div", "span",
        "hr", "sub", "sup", "figure", "figcaption"
    };

    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "src", "alt", "title", "class", "id", "target", "rel",
        "width", "height", "colspan", "rowspan"
    };

    private static readonly Regex TagRegex = new(@"<(/?)(\w+)([^>]*)>", RegexOptions.Compiled);
    private static readonly Regex AttributeRegex = new(@"(\w+)\s*=\s*""([^""]*)""|(\w+)\s*=\s*'([^']*)'", RegexOptions.Compiled);
    private static readonly Regex ScriptRegex = new(@"<script[^>]*>.*?</script>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventRegex = new(@"\bon\w+\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        html = ScriptRegex.Replace(html, string.Empty);
        html = EventRegex.Replace(html, string.Empty);

        html = TagRegex.Replace(html, match =>
        {
            var isClosing = match.Groups[1].Value == "/";
            var tagName = match.Groups[2].Value;
            var attrs = match.Groups[3].Value;

            if (!AllowedTags.Contains(tagName))
                return string.Empty;

            if (isClosing)
                return $"</{tagName}>";

            var sanitizedAttrs = AttributeRegex.Replace(attrs, attrMatch =>
            {
                var name = (attrMatch.Groups[1].Value + attrMatch.Groups[3].Value).ToLowerInvariant();
                var value = attrMatch.Groups[2].Value + attrMatch.Groups[4].Value;

                if (!AllowedAttributes.Contains(name))
                    return string.Empty;

                if (name == "href" && (value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)))
                    return string.Empty;

                if (name == "src" && (value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)))
                    return string.Empty;

                var safeValue = value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
                return $" {name}=\"{safeValue}\"";
            });

            return $"<{tagName}{sanitizedAttrs}>";
        });

        return html;
    }
}
