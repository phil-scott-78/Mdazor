using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class AutoLinksExtensionTests
{
    [Fact]
    public void AutoLinks_ShouldWorkOutsideComponents()
    {
        // Setup pipeline with AutoLinks enabled
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAutoLinks() // Explicitly add AutoLinks
            .Build();

        var markdown = """
                       This is a http://www.google.com URL and https://www.google.com
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify AutoLinks works outside components
        result.ShouldContain("<a href=\"http://www.google.com\">http://www.google.com</a>");
        result.ShouldContain("<a href=\"https://www.google.com\">https://www.google.com</a>");
    }

    [Fact]
    public void AutoLinks_ShouldWorkInsideComponents()
    {
        // Setup services with AutoLinks in pipeline
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAutoLinks() // Explicitly add AutoLinks
            .Build();

        var markdown = """
                       <TestCard title="Links Test">
                       This is a http://www.google.com URL and https://www.google.com
                       
                       Also check out https://github.com/lunet-io/markdig for more info.
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify component renders
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>Links Test</h3>");
        
        // Verify AutoLinks works inside the component content
        result.ShouldContain("<a href=\"http://www.google.com\">http://www.google.com</a>");
        result.ShouldContain("<a href=\"https://www.google.com\">https://www.google.com</a>");
        result.ShouldContain("<a href=\"https://github.com/lunet-io/markdig\">https://github.com/lunet-io/markdig</a>");
    }

    [Fact]
    public void AutoLinks_WithNestedComponents_ShouldProcessAllLevels()
    {
        // Setup services
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>()
            .AddMdazorComponent<TestAlert>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAutoLinks()
            .Build();

        var markdown = """
                       <TestCard title="Outer Component">
                       Check out this site: https://example.com
                       
                       <TestAlert type="info">
                       Alert with a link: http://www.microsoft.com and https://www.dotnet.org
                       </TestAlert>
                       
                       More content with https://www.github.com
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify components render
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<div class=\"test-alert info\">");
        
        // Verify AutoLinks works at all nesting levels
        result.ShouldContain("<a href=\"https://example.com\">https://example.com</a>");
        result.ShouldContain("<a href=\"http://www.microsoft.com\">http://www.microsoft.com</a>");
        result.ShouldContain("<a href=\"https://www.dotnet.org\">https://www.dotnet.org</a>");
        result.ShouldContain("<a href=\"https://www.github.com\">https://www.github.com</a>");
    }

    // Test component for AutoLinks testing
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
            
            if (ChildContent != null)
            {
                builder.OpenElement(4, "div");
                builder.AddAttribute(5, "class", "card-content");
                builder.AddContent(6, ChildContent);
                builder.CloseElement();
            }
            
            builder.CloseElement();
        }
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