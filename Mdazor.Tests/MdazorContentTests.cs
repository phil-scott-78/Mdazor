using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class MdazorContentTests
{
    private static (HtmlRenderer renderer, IServiceProvider services) BuildHost(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestAlert>()
            .AddMdazorComponent<TestIcon>();
        configure?.Invoke(services);
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<HtmlRenderer>(), sp);
    }

    private static string Render(HtmlRenderer renderer, string markdown)
    {
        return AsyncHelper.RunSync(async () => await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Markdown"] = markdown });
            var output = await renderer.RenderComponentAsync<MdazorContent>(parameters);
            return output.ToHtmlString();
        }));
    }

    [Fact]
    public void PlainMarkdown_RendersAsHtml()
    {
        var (renderer, _) = BuildHost();
        var html = Render(renderer, "**bold** and *italic*");

        html.ShouldContain("<strong>bold</strong>");
        html.ShouldContain("<em>italic</em>");
        html.ShouldContain("<p>");
    }

    [Fact]
    public void Heading_RendersAsHtml()
    {
        var (renderer, _) = BuildHost();
        var html = Render(renderer, "# Hello\n\nworld");

        html.ShouldContain("<h1>Hello</h1>");
        html.ShouldContain("<p>world</p>");
    }

    [Fact]
    public void BlockLevelComponent_RendersComponent()
    {
        var (renderer, _) = BuildHost();
        var markdown = """
                       <TestAlert type="info">
                       Hello **world**
                       </TestAlert>
                       """;
        var html = Render(renderer, markdown);

        html.ShouldContain("<div class=\"test-alert info\">");
        html.ShouldContain("<strong>world</strong>");
    }

    [Fact]
    public void MidParagraphInlineComponent_KeepsSurroundingText()
    {
        var (renderer, _) = BuildHost();
        var html = Render(renderer, "Before <TestIcon name=\"star\" /> after.");

        html.ShouldContain("Before");
        html.ShouldContain("<span class=\"icon icon-star\"");
        html.ShouldContain("after.");
    }

    [Fact]
    public void UnknownInlineComponent_FallsBackToTag()
    {
        var (renderer, _) = BuildHost();
        var html = Render(renderer, "Look at <Mystery name=\"x\" /> here.");

        // Unknown components fall back to a lowercase HTML tag.
        html.ShouldContain("<mystery");
        html.ShouldContain("Look at");
        html.ShouldContain("here.");
    }

    [Fact]
    public void KeyAttribute_IsNotEmittedAsParameter()
    {
        // key="..." should be consumed by SetKey, not flow through as a component parameter.
        // If it leaked through, the literal "abc123" would appear in the rendered HTML.
        var (renderer, _) = BuildHost();
        var markdown = """
                       <TestAlert type="info" key="abc123">
                       x
                       </TestAlert>
                       """;
        var html = Render(renderer, markdown);

        html.ShouldContain("<div class=\"test-alert info\">");
        html.ShouldNotContain("abc123");
    }

    [Fact]
    public void StaticPath_StillWorksAfterRefactor()
    {
        // Sanity check that Markdown.ToHtml(...) (the existing string API) still produces the
        // same static HTML — proves the refactor of ConvertAttributeValue/ChildContentSplitter
        // didn't change the static-path output.
        var (_, services) = BuildHost();
        var pipeline = new MarkdownPipelineBuilder().UseMdazor(services).Build();
        var markdown = """
                       <TestAlert type="info">
                       hi
                       </TestAlert>
                       """;
        var html = Markdown.ToHtml(markdown, pipeline);

        html.ShouldContain("<div class=\"test-alert info\">");
        html.ShouldContain("hi");
    }

    [Fact]
    public void ComponentRegisteredWithRenderMode_EmitsRenderModeFrame()
    {
        // Proves AddComponentRenderMode is actually called: a standalone HtmlRenderer cannot
        // honor InteractiveWebAssemblyRenderMode and throws NotSupportedException. In a real
        // Blazor Web App host with .AddInteractiveWebAssemblyRenderMode(), the same frame
        // produces the SSR markers that bootstrap the WASM client.
        var (renderer, _) = BuildHost(svc =>
            svc.AddMdazorComponent<TestBadge>(RenderMode.InteractiveWebAssembly));

        var ex = Should.Throw<NotSupportedException>(() => Render(renderer, "<TestBadge text=\"OK\" />"));
        ex.Message.ShouldContain("InteractiveWebAssemblyRenderMode");
    }

    private class TestAlert : ComponentBase
    {
        [Parameter] public string Type { get; set; } = "info";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", $"test-alert {Type}");
            builder.AddContent(2, ChildContent);
            builder.CloseElement();
        }
    }

    private class TestIcon : ComponentBase
    {
        [Parameter] public string Name { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", $"icon icon-{Name}");
            builder.CloseElement();
        }
    }

    private class TestBadge : ComponentBase
    {
        [Parameter] public string Text { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "badge");
            builder.AddContent(2, Text);
            builder.CloseElement();
        }
    }
}
