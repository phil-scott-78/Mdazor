using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class IntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IComponentRegistry _componentRegistry;
    private readonly MarkdownPipeline _pipeline;

    public IntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MarkdownPipeline>(new MarkdownPipelineBuilder()
            .UseMdazor()
            .Build());
        services.AddMdazor()
            .AddMdazorComponent<TestCard>()
            .AddMdazorComponent<TestAlert>();

        
        _serviceProvider = services.BuildServiceProvider();
        _componentRegistry = _serviceProvider.GetRequiredService<IComponentRegistry>();
        _pipeline = _serviceProvider.GetRequiredService<MarkdownPipeline>();
    }

    [Fact]
    public void ComponentRegistry_ShouldHaveTestComponents()
    {
        // Verify components are registered
        _componentRegistry.IsRegistered("TestCard").ShouldBeTrue();
        _componentRegistry.IsRegistered("TestAlert").ShouldBeTrue();
        _componentRegistry.GetComponentType("TestCard").ShouldBe(typeof(TestCard));
    }

    [Fact]
    public void RenderSimpleComponent_ShouldProduceCorrectHtml()
    {
        var markdown = """
                       <TestCard title="Hello World" />
                       """;

        var result = RenderMarkdownToHtml(markdown);
        
        // Test that component was recognized and rendered (not fallback)
        result.ShouldNotContain("<testcard");
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>Hello World</h3>");
        result.ShouldContain("</div>");
    }

    [Fact]
    public void RenderComponentWithContent_ShouldRenderChildContent()
    {
        var markdown = """
                       <TestCard title="My Card">
                       This is **bold** text and this is *italic*.
                       
                       - Item 1
                       - Item 2
                       </TestCard>
                       """;

        var result = RenderMarkdownToHtml(markdown);

        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>My Card</h3>");
        result.ShouldContain("<strong>bold</strong>");
        result.ShouldContain("<em>italic</em>");
        result.ShouldContain("<li>Item 1</li>");
        result.ShouldContain("<li>Item 2</li>");
    }

    [Fact]
    public void RenderNestedComponents_ShouldRenderBothComponents()
    {
        var markdown = """
                       <TestCard title="Outer Card">
                       # Heading in Card
                       
                       <TestAlert type="warning">
                       Alert with **bold** text!
                       </TestAlert>
                       
                       More content after alert.
                       </TestCard>
                       """;

        var result = RenderMarkdownToHtml(markdown);

        // Outer card
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>Outer Card</h3>");
        
        // Nested alert
        result.ShouldContain("<div class=\"test-alert warning\">");
        result.ShouldContain("Alert with <strong>bold</strong> text!");
        
        // Markdown processing
        result.ShouldContain("<h1>Heading in Card</h1>");
        result.ShouldContain("More content after alert.");
    }

    [Fact]
    public void RenderUnknownComponent_ShouldFallbackToHtml()
    {
        var markdown = """
                       <UnknownComponent title="test" data-id="123">
                       Some content here
                       </UnknownComponent>
                       """;

        var result = RenderMarkdownToHtml(markdown);

        result.ShouldContain("<unknowncomponent title=\"test\" data-id=\"123\">");
        result.ShouldContain("Some content here");
        result.ShouldContain("</unknowncomponent>");
    }

    [Fact]
    public void RenderMixedContent_ShouldHandleComplexScenarios()
    {
        var markdown = """
                       # Main Heading
                       
                       Regular paragraph with **bold** text.
                       
                       <TestCard Title="Feature Card">
                       ## Sub Heading
                       
                       Here's a nested alert:
                       
                       <TestAlert type="info">
                       This is an informational alert with a [link](https://example.com).
                       </TestAlert>
                       
                       And here's an unknown component:
                       <CustomWidget id="widget1" />
                       
                       Final paragraph in the card.
                       </TestCard>
                       
                       ## Another Section
                       
                       <TestAlert Type="danger">
                       Standalone danger alert!
                       </TestAlert>
                       """;

        var result = RenderMarkdownToHtml(markdown);

        // Main content
        result.ShouldContain("<h1>Main Heading</h1>");
        result.ShouldContain("Regular paragraph with <strong>bold</strong> text.");
        
        // Card component
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>Feature Card</h3>");
        result.ShouldContain("<h2>Sub Heading</h2>");
        
        // Nested alert
        result.ShouldContain("<div class=\"test-alert info\">");
        result.ShouldContain("<a href=\"https://example.com\">link</a>");
        
        // Unknown component fallback
        result.ShouldContain("<customwidget id=\"widget1\" />");
        
        // Standalone alert
        result.ShouldContain("<div class=\"test-alert danger\">");
        result.ShouldContain("Standalone danger alert!");
    }

    [Fact]
    public void RenderComponentWithSpecialCharacters_ShouldEscapeCorrectly()
    {
        var markdown = """
                       <TestCard title="Title with &quot;quotes&quot; &amp; symbols">
                       Content with <em>HTML</em> and &lt;script&gt; tags.
                       </TestCard>
                       """;

        var result = RenderMarkdownToHtml(markdown);

        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>Title with &quot;quotes&quot; &amp; symbols</h3>");
        result.ShouldContain("Content with <em>HTML</em> and &lt;script&gt; tags.");
    }

    private string RenderMarkdownToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        
        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider);
        renderer.Render(document);
        
        return writer.ToString().Trim();
    }

    // Test components for integration testing
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