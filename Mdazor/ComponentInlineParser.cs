using Markdig.Helpers;
using Markdig.Parsers;
using System.Text.RegularExpressions;

namespace Mdazor;

public partial class ComponentInlineParser : InlineParser
{
    private static readonly Regex ComponentSelfClosingTagRegex = ComponentSelfClosingTagRegexDef();
    private static readonly Regex AttributeRegex = AttributeRegexDef();

    public ComponentInlineParser()
    {
        OpeningCharacters = ['<'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var text = slice.ToString();
        var match = ComponentSelfClosingTagRegex.Match(text);
        
        if (!match.Success)
        {
            return false;
        }

        var componentName = match.Groups[1].Value;
        var attributesText = match.Groups[2].Value;
        var attributes = ParseAttributes(attributesText);

        var componentInline = new ComponentInline
        {
            ComponentName = componentName,
            Attributes = attributes,
            Span = new Markdig.Syntax.SourceSpan(slice.Start, slice.Start + match.Length - 1)
        };

        processor.Inline = componentInline;
        slice.Start += match.Length;

        return true;
    }

    private static Dictionary<string, string> ParseAttributes(string attributesText)
    {
        var attributes = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(attributesText))
        {
            return attributes;
        }

        var matches = AttributeRegex.Matches(attributesText);
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var value = System.Web.HttpUtility.HtmlDecode(match.Groups[2].Value);
            attributes[name] = value;
        }

        return attributes;
    }

    [GeneratedRegex("""^<([A-Z][a-zA-Z0-9]*)\s*([^>]*)/>\s*""", RegexOptions.Compiled)]
    private static partial Regex ComponentSelfClosingTagRegexDef();
    [GeneratedRegex("""([\w-]+)=["']([^"']*)["']""", RegexOptions.Compiled)]
    private static partial Regex AttributeRegexDef();
}