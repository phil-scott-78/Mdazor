using Markdig.Syntax.Inlines;

namespace Mdazor;

public class ComponentInline : Inline
{
    public string ComponentName { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}