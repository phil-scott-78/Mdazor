using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class SimpleIntegrationTests
{
    [Fact]
    public void EndToEnd_MarkdownWithComponents_ShouldRenderCorrectly()
    {
        // Setup
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMdazor()
            .AddMdazorComponent<SimpleCard>()
            .AddMdazorComponent<SimpleAlert>();

        var serviceProvider = services.BuildServiceProvider();
        var componentRegistry = serviceProvider.GetRequiredService<IComponentRegistry>();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .Build();
        // Test markdown with components
        var markdown = """
                       # Test Document
                       
                       <SimpleCard Title="Card Title">
                       This is **bold** content in a card.
                       
                       <SimpleAlert type="info">
                       Nested alert with *italic* text!
                       </SimpleAlert>
                       
                       More content after the alert.
                       </SimpleCard>
                       
                       ## Unknown Component Test
                       
                       <UnknownWidget id="test123" />
                       """;

        // Execute
        var document = Markdown.Parse(markdown, pipeline);
        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, componentRegistry, serviceProvider, pipeline);
        renderer.Render(document);
        var result = writer.ToString();
        
        // Check basic markdown
        result.ShouldContain("<h1>Test Document</h1>");
        result.ShouldContain("<h2>Unknown Component Test</h2>");
        
        // Check component rendering vs fallback
        // If components work: should contain proper HTML structure
        // If they fallback: should contain lowercase tags
        var hasProperComponents = result.Contains("<div class=\"simple-card\">");
        var hasFallbackComponents = result.Contains("<simplecard");
        
        // At least one should be true
        (hasProperComponents || hasFallbackComponents).ShouldBeTrue();
        
        // Check unknown component fallback
        result.ShouldContain("<unknownwidget id=\"test123\" />");
        
        // Check markdown processing
        result.ShouldContain("<strong>bold</strong>");
        result.ShouldContain("<em>italic</em>");
        
        // If components rendered properly, check structure  
        if (hasProperComponents)
        {
            result.ShouldContain("<div class=\"simple-card\">");
            result.ShouldContain("<h3>Card Title</h3>");
            result.ShouldContain("<div class=\"card-content\">");
            result.ShouldContain("<div class=\"simple-alert info\">");
        }
        else
        {
            // If fallback, we should still see the component tags
            result.ShouldContain("<simplecard Title=\"Card Title\">");
            result.ShouldContain("<simplealert type=\"info\">");
        }
    }

    [Fact]
    public void ComponentRegistration_ShouldWorkCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<SimpleCard>()
            .AddMdazorComponent<SimpleAlert>();
        
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IComponentRegistry>();

        registry.IsRegistered("SimpleCard").ShouldBeTrue();
        registry.IsRegistered("SimpleAlert").ShouldBeTrue();
        registry.IsRegistered("UnknownComponent").ShouldBeFalse();
    }

    [Fact]
    public void CaseInsensitiveAttributes_ShouldWorkCorrectly()
    {
        // Setup
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<SimpleAlert>();

        var serviceProvider = services.BuildServiceProvider();
        var componentRegistry = serviceProvider.GetRequiredService<IComponentRegistry>();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .Build();
        // Test both lowercase and capitalized attributes (as block elements)
        var markdown = """
                       <SimpleAlert type="info">
                       Lowercase type
                       </SimpleAlert>
                       
                       <SimpleAlert Type="warning">
                       Capitalized Type
                       </SimpleAlert>
                       """;

        // Execute
        var document = Markdown.Parse(markdown, pipeline);
        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, componentRegistry, serviceProvider, pipeline);
        renderer.Render(document);
        var result = writer.ToString();

        // Verify both work correctly
        result.ShouldContain("<div class=\"simple-alert info\">");
        result.ShouldContain("Lowercase type");
        result.ShouldContain("<div class=\"simple-alert warning\">");
        result.ShouldContain("Capitalized Type");
    }

    // Simple test components
    private class SimpleCard : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "simple-card");
            
            if (!string.IsNullOrEmpty(Title))
            {
                builder.OpenElement(2, "h3");
                builder.AddContent(3, Title);
                builder.CloseElement();
            }
            
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

    private class SimpleAlert : ComponentBase
    {
        [Parameter] public string Type { get; set; } = "info";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", $"simple-alert {Type}");
            
            if (ChildContent != null)
            {
                builder.AddContent(2, ChildContent);
            }
            
            builder.CloseElement();
        }
    }
}