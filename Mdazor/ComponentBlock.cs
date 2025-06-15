using Markdig.Parsers;
using Markdig.Syntax;

namespace Mdazor;

public class ComponentBlock : ContainerBlock
{
    public string ComponentName { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
    public bool IsSelfClosing { get; set; }
    
    public ComponentBlock(BlockParser parser) : base(parser)
    {
    }
}