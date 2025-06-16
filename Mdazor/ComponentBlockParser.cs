using Markdig.Parsers;
using Markdig.Syntax;
using System.Text.RegularExpressions;

namespace Mdazor;

public partial class ComponentBlockParser : BlockParser
{
    private static readonly Regex ComponentOpenTagRegex = ComponentOpenTagRegexDef();
    private static readonly Regex ComponentSelfClosingTagRegex = ComponentSelfClosingTagRegexDef();
    private static readonly Regex ComponentCloseTagRegex = ComponentCloseTagRegexDev();
    private static readonly Regex AttributeRegex = AttributeRegexDef();

    public ComponentBlockParser()
    {
        OpeningCharacters = ['<'];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        var line = processor.Line;
        var lineText = line.ToString();

        // Check for a self-closing component tag
        var selfClosingMatch = ComponentSelfClosingTagRegex.Match(lineText);
        if (selfClosingMatch.Success)
        {
            var componentName = selfClosingMatch.Groups[1].Value;
            var attributesText = selfClosingMatch.Groups[2].Value;
            var attributes = ParseAttributes(attributesText);

            var componentBlock = new ComponentBlock(this)
            {
                ComponentName = componentName,
                Attributes = attributes,
                IsSelfClosing = true
            };

            processor.NewBlocks.Push(componentBlock);
            return BlockState.BreakDiscard;
        }

        // Check for opening component tag
        var openMatch = ComponentOpenTagRegex.Match(lineText);
        if (openMatch.Success)
        {
            var componentName = openMatch.Groups[1].Value;
            var attributesText = openMatch.Groups[2].Value;
            var attributes = ParseAttributes(attributesText);

            var componentBlock = new ComponentBlock(this)
            {
                ComponentName = componentName,
                Attributes = attributes,
                IsSelfClosing = false
            };

            processor.NewBlocks.Push(componentBlock);
            return BlockState.ContinueDiscard;
        }

        return BlockState.None;
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        if (block is not ComponentBlock componentBlock || componentBlock.IsSelfClosing)
        {
            return BlockState.None;
        }

        var line = processor.Line;
        var lineText = line.ToString();

        // Check for closing tag
        var closeMatch = ComponentCloseTagRegex.Match(lineText);
        if (closeMatch.Success && closeMatch.Groups[1].Value == componentBlock.ComponentName)
        {
            return BlockState.BreakDiscard;
        }

        // Preserve original column position to maintain indentation
        // This ensures whitespace is preserved for nested content like lists and code blocks
        processor.GoToColumn(processor.ColumnBeforeIndent);

        // Continue parsing content
        return BlockState.Continue;
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

    [GeneratedRegex("""^<([A-Z][a-zA-Z0-9]*)\s*([^>]*)>$""", RegexOptions.Compiled)]
    private static partial Regex ComponentOpenTagRegexDef();
    [GeneratedRegex("""^<([A-Z][a-zA-Z0-9]*)\s*([^>]*)/>\s*$""", RegexOptions.Compiled)]
    private static partial Regex ComponentSelfClosingTagRegexDef();
    [GeneratedRegex("""^</([A-Z][a-zA-Z0-9]*)>\s*$""", RegexOptions.Compiled)]
    private static partial Regex ComponentCloseTagRegexDev();
    [GeneratedRegex("([\\w-]+)=[\"']([^\"']*)[\"']", RegexOptions.Compiled)]
    private static partial Regex AttributeRegexDef();
}