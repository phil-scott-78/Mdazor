using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class MarkdownToHtmlTests
{
    [Fact]
    public void MarkdownToHtml_WithComponents_ShouldRenderCorrectly()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestAlert>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<MarkdownPipeline>();

        // Test markdown with component
        var markdown = """
                       # Test Document
                       
                       <TestAlert type="info">
                       This is an alert with **bold** text!
                       </TestAlert>
                       """;

        // Execute using standard Markdown.ToHtml
        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify
        result.ShouldContain("<h1>Test Document</h1>");
        result.ShouldContain("<div class=\"test-alert info\">");
        result.ShouldContain("<strong>bold</strong>");
    }

    [Fact]
    public void MarkdownToHtml_WithUnknownComponent_ShouldFallback()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<MarkdownPipeline>();

        // Test markdown with unknown component
        var markdown = """
                       <UnknownComponent title="test">
                       Some content
                       </UnknownComponent>
                       """;

        // Execute using standard Markdown.ToHtml
        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify fallback behavior
        result.ShouldContain("<unknowncomponent title=\"test\">");
        result.ShouldContain("Some content");
        result.ShouldContain("</unknowncomponent>");
    }

    private class TestAlert : ComponentBase
    {
        [Parameter] public string Type { get; set; } = "info";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", $"test-alert {Type}");
            
            if (ChildContent != null)
            {
                builder.AddContent(2, ChildContent);
            }
            
            builder.CloseElement();
        }
    }
}