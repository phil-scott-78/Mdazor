using Markdig.Renderers.Html;
using MarkdigHtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace Mdazor;

// Emits a sentinel-delimited index token around each ComponentInline so the
// surrounding paragraph HTML can be split and ComponentInline nodes can be
// reinserted as real Razor components by MarkdownRenderTreeWriter.
//
// Sentinel is U+001E (record separator) — non-printable, never produced by
// Markdig's standard renderers, and not in Markdig's HTML escape set so it
// survives WriteEscape calls intact.
internal class PlaceholderInlineRenderer : HtmlObjectRenderer<ComponentInline>
{
    public const string Sentinel = "";

    private readonly List<ComponentInline> _captured;

    public PlaceholderInlineRenderer(List<ComponentInline> captured)
    {
        _captured = captured;
    }

    protected override void Write(MarkdigHtmlRenderer renderer, ComponentInline inline)
    {
        var index = _captured.Count;
        _captured.Add(inline);
        renderer.Write(Sentinel);
        renderer.Write(index.ToString());
        renderer.Write(Sentinel);
    }
}
