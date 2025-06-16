using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class InlineComponentTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IComponentRegistry _componentRegistry;
    private readonly MarkdownPipeline _pipeline;

    public InlineComponentTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestIcon>()
            .AddMdazorComponent<TestBadge>()
            .AddMdazorComponent<TestSpinner>();
        
        _serviceProvider = services.BuildServiceProvider();
        _componentRegistry = _serviceProvider.GetRequiredService<IComponentRegistry>();
        _pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(_serviceProvider)
            .Build();
    }

    [Fact]
    public void InlineComponent_BasicSelfClosing_ShouldRender()
    {
        var markdown = """
                       Here is an icon: <TestIcon name="star" size="16" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<p>Here is an icon: ");
        result.ShouldContain("<span class=\"icon icon-star\" style=\"font-size: 16px;\"></span>");
        result.ShouldContain("</p>");
    }

    [Fact]
    public void InlineComponent_WithMultipleAttributes_ShouldRender()
    {
        var markdown = """
                       Status: <TestBadge text="Active" color="green" rounded="true" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<p>Status: ");
        result.ShouldContain("<span class=\"badge badge-green rounded\">Active</span>");
        result.ShouldContain("</p>");
    }

    [Fact]
    public void InlineComponent_WithHyphenatedAttributes_ShouldRender()
    {
        var markdown = """
                       Loading: <TestSpinner data-testid="loading" aria-label="Loading content" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<p>Loading: ");
        // The component should render even if it doesn't use the hyphenated attributes
        result.ShouldContain("<div class=\"spinner spinning\"");
        result.ShouldContain("</p>");
    }

    [Fact]
    public void InlineComponent_CaseInsensitiveAttributes_ShouldWork()
    {
        var markdown = """
                       Icons: <TestIcon name="home" Size="20" /> and <TestIcon Name="user" size="18" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<span class=\"icon icon-home\" style=\"font-size: 20px;\"></span>");
        result.ShouldContain("<span class=\"icon icon-user\" style=\"font-size: 18px;\"></span>");
    }

    [Fact]
    public void InlineComponent_WithSpecialCharacters_ShouldEscape()
    {
        var markdown = """
                       Badge: <TestBadge text="&quot;Special&quot; &amp; chars" color="blue" />
                       """;

        var result = RenderMarkdown(markdown);

        // The HTML entities in the attribute get decoded when parsed, then re-escaped when rendered
        result.ShouldContain("<span class=\"badge badge-blue\">&quot;Special&quot; &amp; chars</span>");
    }

    [Fact]
    public void InlineComponent_MultipleinSameParagraph_ShouldRender()
    {
        var markdown = """
                       Here are some icons: <TestIcon name="home" size="16" /> <TestIcon name="star" size="16" /> and <TestIcon name="user" size="16" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<span class=\"icon icon-home\" style=\"font-size: 16px;\"></span>");
        result.ShouldContain("<span class=\"icon icon-star\" style=\"font-size: 16px;\"></span>");
        result.ShouldContain("<span class=\"icon icon-user\" style=\"font-size: 16px;\"></span>");
    }

    [Fact]
    public void InlineComponent_MixedWithMarkdown_ShouldRender()
    {
        var markdown = """
                       This is **bold** text with an <TestIcon name="star" size="14" /> icon and *italic* text.
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<strong>bold</strong>");
        result.ShouldContain("<span class=\"icon icon-star\" style=\"font-size: 14px;\"></span>");
        result.ShouldContain("<em>italic</em>");
    }

    [Fact]
    public void InlineComponent_InList_ShouldRender()
    {
        var markdown = """
                       Features:
                       - Home <TestIcon name="home" size="12" />
                       - Favorites <TestIcon name="star" size="12" />
                       - Profile <TestIcon name="user" size="12" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<li>Home <span class=\"icon icon-home\" style=\"font-size: 12px;\"></span></li>");
        result.ShouldContain("<li>Favorites <span class=\"icon icon-star\" style=\"font-size: 12px;\"></span></li>");
        result.ShouldContain("<li>Profile <span class=\"icon icon-user\" style=\"font-size: 12px;\"></span></li>");
    }

    [Fact]
    public void InlineComponent_InHeading_ShouldRender()
    {
        var markdown = """
                       ## Welcome <TestIcon name="star" size="20" /> Home
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<h2>Welcome <span class=\"icon icon-star\" style=\"font-size: 20px;\"></span>Home</h2>");
    }

    [Fact]
    public void InlineComponent_UnknownComponent_ShouldFallback()
    {
        var markdown = """
                       Here is an unknown: <UnknownIcon name="test" size="16" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<unknownicon name=\"test\" size=\"16\" />");
    }

    [Fact]
    public void InlineComponent_UnknownWithHyphenatedAttributes_ShouldPreserveInFallback()
    {
        var markdown = """
                       Unknown: <UnknownWidget data-testid="test" aria-label="widget" />
                       """;

        var result = RenderMarkdown(markdown);

        // Should preserve hyphenated attributes in fallback HTML
        result.ShouldContain("<unknownwidget data-testid=\"test\" aria-label=\"widget\" />");
    }

    [Fact]
    public void InlineComponent_WithTypeConversion_ShouldWork()
    {
        var markdown = """
                       Spinner: <TestSpinner size="24" enabled="true" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"spinner spinning\" style=\"width: 24px; height: 24px;\"></div>");
    }

    [Fact]
    public void InlineComponent_UsingMarkdownToHtml_ShouldWork()
    {
        var markdown = """
                       Simple test: <TestIcon name="home" size="16" />
                       """;

        // Test using Markdown.ToHtml directly
        var result = Markdown.ToHtml(markdown, _pipeline);

        result.ShouldContain("<span class=\"icon icon-home\" style=\"font-size: 16px;\"></span>");
    }

    private string RenderMarkdown(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        
        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider, _pipeline);
        renderer.Render(document);
        
        return writer.ToString().Trim();
    }

    // Test components for inline testing
    private class TestIcon : ComponentBase
    {
        [Parameter] public string Name { get; set; } = "";
        [Parameter] public int Size { get; set; } = 16;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", $"icon icon-{Name}");
            builder.AddAttribute(2, "style", $"font-size: {Size}px;");
            builder.CloseElement();
        }
    }

    private class TestBadge : ComponentBase
    {
        [Parameter] public string Text { get; set; } = "";
        [Parameter] public string Color { get; set; } = "gray";
        [Parameter] public bool Rounded { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cssClass = $"badge badge-{Color}";
            if (Rounded) cssClass += " rounded";

            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", cssClass);
            builder.AddContent(2, Text);
            builder.CloseElement();
        }
    }

    private class TestSpinner : ComponentBase
    {
        [Parameter] public int Size { get; set; } = 16;
        [Parameter] public bool Enabled { get; set; } = true;
        
        // Support exact hyphenated attribute names
        [Parameter] public string DataTestid { get; set; } = "";
        [Parameter] public string AriaLabel { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var cssClass = "spinner";
            if (Enabled) cssClass += " spinning";

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", cssClass);
            builder.AddAttribute(2, "style", $"width: {Size}px; height: {Size}px;");
            
            if (!string.IsNullOrEmpty(DataTestid))
                builder.AddAttribute(3, "data-testid", DataTestid);
            
            if (!string.IsNullOrEmpty(AriaLabel))
                builder.AddAttribute(4, "aria-label", AriaLabel);
            
            builder.CloseElement();
        }
    }
}