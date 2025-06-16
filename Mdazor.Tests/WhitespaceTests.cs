using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class WhitespaceTests
{
   
    [Fact]
    public void Whitespace_should_be_respected()
    {
        // Setup services with AutoLinks in pipeline
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions() // Add extensions for proper code block handling
            .Build();

        var markdown = """
                       - outside a
                       - outside b level 1
                         - outside b level 2
                       
                       <TestCard title="Whitespace Test">
                       - inside a
                       - inside b level 1
                         - inside b level 2
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Basic test - ensure components render and preserve content structure
        result.ShouldContain("<div class=\"test-card\">");
        result.ShouldContain("outside a");
        result.ShouldContain("inside a");
        result.ShouldContain("outside b level 1");
        result.ShouldContain("inside b level 1");
    }

    [Fact]
    public void Nested_lists_should_preserve_indentation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions()
            .Build();

        var markdown = """
                       - outside a
                       - outside b level 1
                         - outside b level 2
                           - outside b level 3
                       
                       <TestCard title="Nested Lists Test">
                       - inside a
                       - inside b level 1
                         - inside b level 2
                           - inside b level 3
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Check that both outside and inside lists have proper nesting
        result.ShouldContain("<ul>");
        result.ShouldContain("<li>outside a</li>");
        result.ShouldContain("<li>inside a</li>");
        
        // Check for nested list structure - this will fail if whitespace is stripped
        result.ShouldContain("<li>outside b level 1\n<ul>\n<li>outside b level 2");
        result.ShouldContain("<li>inside b level 1\n<ul>\n<li>inside b level 2");
    }

    [Fact]
    public void Pre_tags_should_preserve_whitespace()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions()
            .Build();

        var markdown = """
                       <TestCard title="Pre Tag Test">
                       <pre>
                           Line 1 with    multiple   spaces
                             Line 2 with different indentation
                       	Line 3 with tab
                       </pre>
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Pre tags should preserve all whitespace exactly
        result.ShouldContain("Line 1 with    multiple   spaces");
        result.ShouldContain("  Line 2 with different indentation");
        result.ShouldContain("\tLine 3 with tab");
    }

    [Fact]
    public void Code_blocks_with_complex_indentation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions()
            .Build();

        var markdown = """
                       <TestCard title="Complex Code Block">
                       ```csharp
                       public class Example
                       {
                           public void Method()
                           {
                               if (condition)
                               {
                                   // Nested comment
                                   DoSomething();
                               }
                           }
                       }
                       ```
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Code blocks should preserve structure
        result.ShouldContain("public class Example");
        result.ShouldContain("public void Method()");
        result.ShouldContain("if (condition)");
        result.ShouldContain("// Nested comment");
        result.ShouldContain("DoSomething();");
    }

    [Fact]
    public void Mixed_indentation_with_tabs_and_spaces()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions()
            .Build();

        var markdown = """
                       <TestCard title="Mixed Indentation">
                       - List item 1
                       	- Tab indented item
                           - Space indented item
                       		- Mixed tab/space item
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Should create properly nested lists regardless of tab/space mixing
        result.ShouldContain("<li>List item 1");
        result.ShouldContain("<li>Tab indented item");
        result.ShouldContain("<li>Space indented item");
        result.ShouldContain("<li>Mixed tab/space item");
        
        // Should have nested <ul> elements
        var nestedUlCount = System.Text.RegularExpressions.Regex.Matches(result, "<ul>").Count;
        nestedUlCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void Blockquotes_should_preserve_indentation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions()
            .Build();

        var markdown = """
                       <TestCard title="Blockquote Test">
                       > This is a blockquote
                       > with multiple lines
                       > 
                       > > And a nested blockquote
                       > > with its own content
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Should create proper blockquote structure
        result.ShouldContain("<blockquote>");
        result.ShouldContain("This is a blockquote");
        result.ShouldContain("with multiple lines");
        result.ShouldContain("And a nested blockquote");
        
        // Should have nested blockquotes
        var blockquoteCount = System.Text.RegularExpressions.Regex.Matches(result, "<blockquote>").Count;
        blockquoteCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void Component_whitespace_preserves_content_structure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
        
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .UseAdvancedExtensions()
            .Build();

        var markdown = """
                       <TestCard title="Whitespace Preservation">
                       Line 1 with    multiple   spaces
                       
                           Indented line
                       
                       Back to normal
                       </TestCard>
                       """;

        var result = Markdown.ToHtml(markdown, pipeline);

        // Should preserve the content structure and spacing
        result.ShouldContain("Line 1 with    multiple   spaces");
        result.ShouldContain("Indented line");
        result.ShouldContain("Back to normal");
        result.ShouldContain("<div class=\"test-card\">");
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
}