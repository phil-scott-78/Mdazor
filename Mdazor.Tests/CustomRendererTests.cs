using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class CustomRendererTests
{
    [Fact]
    public void CustomRenderer_ShouldWorkOutsideComponents()
    {
        // Setup pipeline with custom renderer
        var pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseCustomFencedCodeRenderer()
            .Build();

        var markdown = """
                       # Test Document
                       
                       ```csharp
                       var test = "hello";
                       ```
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify custom renderer works outside components
        result.ShouldContain("<div class=\"custom-code-block\">");
        result.ShouldContain("var test = &quot;hello&quot;"); // HTML encoded quotes
        result.ShouldContain("</div>");
        result.ShouldNotContain("<pre><code>"); // Should not use default renderer
    }

    [Fact] 
    public void CustomRenderer_ShouldWorkInsideComponents()
    {
        // Setup services and pipeline with custom renderer
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UsePipeTables()
            .UseCustomFencedCodeRenderer()
            .Build();

        var markdown = """
                       <TestCard title="Code Example">
                       Here's some code:
                       
                       ```csharp
                       var test = "hello";
                       Console.WriteLine(test);
                       ```
                       
                       End of card.
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);
        

        // Verify component renders
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<h3>Code Example</h3>");
        
        // Verify custom renderer works inside the component content
        result.ShouldContain("<div class=\"custom-code-block\">");
        result.ShouldContain("var test = &quot;hello&quot;"); // HTML encoded quotes
        result.ShouldContain("Console.WriteLine(test);");
        result.ShouldContain("</div>");
        result.ShouldNotContain("<pre><code>"); // Should not use default renderer
    }

    [Fact]
    public void CustomRenderer_WithNestedComponents_ShouldWorkAtAllLevels()
    {
        // Setup services and pipeline with custom renderer
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>()
            .AddMdazorComponent<TestAlert>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseCustomFencedCodeRenderer()
            .Build();

        var markdown = """
                       <TestCard title="Outer Component">
                       Outer code block:
                       
                       ```javascript
                       console.log('outer');
                       ```
                       
                       <TestAlert type="info">
                       Alert with code:
                       
                       ```python
                       print('nested')
                       ```
                       </TestAlert>
                       
                       More outer content.
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Verify components render
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("<div class=\"test-alert info\">");
        
        // Verify custom renderer works at both levels
        result.ShouldContain("console.log('outer');"); // Not HTML encoded inside custom renderer
        result.ShouldContain("print('nested')");
        
        // Count occurrences of custom renderer div
        var customDivCount = result.Split("<div class=\"custom-code-block\">").Length - 1;
        customDivCount.ShouldBe(2); // Should have two custom code blocks
        
        result.ShouldNotContain("<pre><code>"); // Should not use default renderer anywhere
    }

    // Test components for custom renderer testing
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

// Custom extension and renderer for testing
public static class CustomFencedCodeExtension
{
    public static MarkdownPipelineBuilder UseCustomFencedCodeRenderer(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready<CustomFencedCodeExtensionImpl>();
        return pipeline;
    }

    private class CustomFencedCodeExtensionImpl : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // No additional setup needed
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                // Insert our custom renderer at the beginning so it has priority
                htmlRenderer.ObjectRenderers.Insert(0, new CustomFencedCodeBlockRenderer());
            }
        }
    }
}

public class CustomFencedCodeBlockRenderer : HtmlObjectRenderer<FencedCodeBlock>
{
    protected override void Write(HtmlRenderer renderer, FencedCodeBlock codeBlock)
    {
        renderer.Write("<div class=\"custom-code-block\">");
        
        if (codeBlock.Lines.Lines != null)
        {
            var code = string.Join(Environment.NewLine, codeBlock.Lines.Lines.Select(line => line.ToString()));
            renderer.WriteEscape(code);
        }
        
        renderer.Write("</div>");
    }
}