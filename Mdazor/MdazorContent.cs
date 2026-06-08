using Markdig;
using Markdig.Syntax;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Mdazor;

public class MdazorContent : ComponentBase
{
    [Parameter, EditorRequired] public string Markdown { get; set; } = "";
    [Parameter] public MarkdownPipeline? Pipeline { get; set; }

    /// <summary>
    /// Host-supplied ambient context surfaced to every rendered component (and its descendants)
    /// as a cascading value. Read it from a component with
    /// <c>[CascadingParameter] public MdazorContext? Context { get; set; }</c>.
    /// </summary>
    [Parameter] public MdazorContext? Context { get; set; }

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

        if (Context is null)
        {
            MarkdownRenderTreeWriter.Write(builder, _doc, Registry, _pipeline);
            return;
        }

        // Wrap the whole document once: a single cascade reaches every real component
        // frame the writer emits (and their descendants).
        builder.OpenComponent<CascadingValue<MdazorContext>>(0);
        builder.AddComponentParameter(1, "Value", Context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b =>
            MarkdownRenderTreeWriter.Write(b, _doc, Registry, _pipeline)));
        builder.CloseComponent();
    }
}
