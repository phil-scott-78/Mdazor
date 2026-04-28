using Markdig.Parsers;
using Markdig.Syntax;

namespace Mdazor;

public class ComponentBlock : ContainerBlock
{
    public string ComponentName { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
    public bool IsSelfClosing { get; set; }

    // Column where the opening tag started. Used by TryContinue so child content
    // is interpreted relative to the component's own indent (e.g. an inner
    // component nested two spaces in shouldn't turn its body into a code block).
    public int OpeningColumn { get; set; }

    public ComponentBlock(BlockParser parser) : base(parser)
    {
    }
}