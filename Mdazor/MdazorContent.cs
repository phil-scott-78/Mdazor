using Markdig;
using Markdig.Syntax;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Mdazor;

public class MdazorContent : ComponentBase
{
    [Parameter, EditorRequired] public string Markdown { get; set; } = "";
    [Parameter] public MarkdownPipeline? Pipeline { get; set; }

    [Inject] private IComponentRegistry Registry { get; set; } = default!;
    [Inject] private MarkdownPipeline DefaultPipeline { get; set; } = default!;

    private MarkdownDocument? _doc;
    private MarkdownPipeline? _pipeline;
    private string? _lastMarkdown;

    protected override void OnParametersSet()
    {
        var pipeline = Pipeline ?? DefaultPipeline;
        if (!ReferenceEquals(pipeline, _pipeline) || Markdown != _lastMarkdown)
        {
            _doc = Markdig.Markdown.Parse(Markdown ?? "", pipeline);
            _pipeline = pipeline;
            _lastMarkdown = Markdown;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_doc is null || _pipeline is null) return;
        MarkdownRenderTreeWriter.Write(builder, _doc, Registry, _pipeline);
    }
}
