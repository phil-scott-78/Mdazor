using Markdig.Syntax.Inlines;

namespace Mdazor;

public class ComponentInline : Inline
{
    public string ComponentName { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();

    // Host-supplied ambient context captured at parse time (see MarkdownParserContext.SetMdazorContext).
    // Surfaced to the rendered component as a cascading MdazorContext value.
    public MdazorContext? Context { get; set; }
}