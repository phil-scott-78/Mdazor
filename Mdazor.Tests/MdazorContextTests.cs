using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class MdazorContextTests
{
    // ---- string path (Markdown.ToHtml / HtmlRenderer) ----

    private static MarkdownPipeline BuildStringPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<ContextProbe>()
            .AddMdazorComponent<TestCard>();
        var sp = services.BuildServiceProvider();
        return new MarkdownPipelineBuilder().UseMdazor(sp).Build();
    }

    private static MarkdownParserContext ParserContext(params (string Key, object? Value)[] pairs)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            values[key] = value;
        }

        return new MarkdownParserContext().SetMdazorContext(values);
    }

    [Fact]
    public void StringPath_BlockComponent_ReceivesContext()
    {
        var pipeline = BuildStringPipeline();
        var context = ParserContext(("CurrentFile", "guide/intro.md"));

        var html = Markdown.ToHtml("<ContextProbe>\nbody\n</ContextProbe>", pipeline, context);

        html.ShouldContain("guide/intro.md");
    }

    [Fact]
    public void StringPath_InlineComponent_ReceivesContext()
    {
        var pipeline = BuildStringPipeline();
        var context = ParserContext(("CurrentFile", "page.md"));

        var html = Markdown.ToHtml("Before <ContextProbe /> after.", pipeline, context);

        html.ShouldContain("Before");
        html.ShouldContain("page.md");
        html.ShouldContain("after.");
    }

    [Fact]
    public void StringPath_NestedComponent_ReceivesContext()
    {
        var pipeline = BuildStringPipeline();
        var context = ParserContext(("CurrentFile", "nested.md"));
        var markdown = """
                       <TestCard title="Outer">
                       Inline here: <ContextProbe />
                       </TestCard>
                       """;

        var html = Markdown.ToHtml(markdown, pipeline, context);

        html.ShouldContain("<div class=\"test-card\">");
        html.ShouldContain("nested.md");
    }

    [Fact]
    public void StringPath_WithoutContext_RendersWithoutError()
    {
        var pipeline = BuildStringPipeline();

        var html = Markdown.ToHtml("<ContextProbe />", pipeline);

        html.ShouldContain("NO-CONTEXT");
    }

    [Fact]
    public void Context_IsCaseInsensitiveWhenDictionaryIs()
    {
        var pipeline = BuildStringPipeline();
        // Stored as "currentfile"; component reads "CurrentFile". Honors the dictionary's comparer.
        var context = ParserContext(("currentfile", "casing.md"));

        var html = Markdown.ToHtml("<ContextProbe />", pipeline, context);

        html.ShouldContain("casing.md");
    }

    // ---- render-tree path (MdazorContent) ----

    private static HtmlRenderer BuildTreeRenderer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<ContextProbe>()
            .AddMdazorComponent<TestCard>();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<HtmlRenderer>();
    }

    private static string RenderTree(HtmlRenderer renderer, string markdown, MdazorContext? context)
    {
        return AsyncHelper.RunSync(async () => await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Markdown"] = markdown,
                ["Context"] = context,
            });
            var output = await renderer.RenderComponentAsync<MdazorContent>(parameters);
            return output.ToHtmlString();
        }));
    }

    [Fact]
    public void RenderTreePath_Component_ReceivesContext()
    {
        var renderer = BuildTreeRenderer();
        var context = new MdazorContext(new Dictionary<string, object?> { ["CurrentFile"] = "tree.md" });

        var html = RenderTree(renderer, "<ContextProbe />", context);

        html.ShouldContain("tree.md");
    }

    [Fact]
    public void RenderTreePath_NestedComponent_ReceivesContext()
    {
        var renderer = BuildTreeRenderer();
        var context = new MdazorContext(new Dictionary<string, object?> { ["CurrentFile"] = "tree-nested.md" });
        var markdown = """
                       <TestCard title="Outer">
                       Inline here: <ContextProbe />
                       </TestCard>
                       """;

        var html = RenderTree(renderer, markdown, context);

        html.ShouldContain("tree-nested.md");
    }

    [Fact]
    public void RenderTreePath_WithoutContext_RendersWithoutError()
    {
        var renderer = BuildTreeRenderer();

        var html = RenderTree(renderer, "<ContextProbe />", null);

        html.ShouldContain("NO-CONTEXT");
    }

    // A component that reads the ambient context via a cascading parameter and echoes a value.
    private class ContextProbe : ComponentBase
    {
        [CascadingParameter] public MdazorContext? Context { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "probe");
            builder.AddContent(2, Context?["CurrentFile"]?.ToString() ?? "NO-CONTEXT");
            if (ChildContent is not null)
            {
                builder.AddContent(3, ChildContent);
            }

            builder.CloseElement();
        }
    }

    private class TestCard : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "test-card");

            builder.OpenElement(2, "h3");
            builder.AddContent(3, Title);
            builder.CloseElement();

            if (ChildContent is not null)
            {
                builder.AddContent(4, ChildContent);
            }

            builder.CloseElement();
        }
    }
}
