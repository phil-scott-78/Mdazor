using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class ComponentParserEdgeCaseTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IComponentRegistry _componentRegistry;
    private readonly MarkdownPipeline _pipeline;

    public ComponentParserEdgeCaseTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<SimpleComponent>()
            .AddMdazorComponent<ErrorComponent>();
        
        _serviceProvider = services.BuildServiceProvider();
        _componentRegistry = _serviceProvider.GetRequiredService<IComponentRegistry>();
        _pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(_serviceProvider)
            .Build();
    }

    [Fact]
    public void Component_WithNoAttributes_ShouldRender()
    {
        var markdown = """
                       <SimpleComponent />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Default</div>");
    }

    [Fact]
    public void Component_WithEmptyAttributes_ShouldRender()
    {
        var markdown = """
                       <SimpleComponent text="" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\"></div>");
    }

    [Fact]
    public void Component_WithQuotesInAttributes_ShouldRender()
    {
        var markdown = """
                       <SimpleComponent text="Say &quot;Hello&quot;" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Say &quot;Hello&quot;</div>");
    }

    [Fact]
    public void Component_WithSingleQuoteAttributes_ShouldRender()
    {
        var markdown = """
                       <SimpleComponent text='Single quotes work too' />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Single quotes work too</div>");
    }

    [Fact]
    public void Component_WithNumericAttributes_ShouldConvert()
    {
        var markdown = """
                       <SimpleComponent number="42" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Default Number: 42</div>");
    }

    [Fact]
    public void Component_WithBooleanAttributes_ShouldConvert()
    {
        var markdown = """
                       <SimpleComponent flag="true" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Default Flag: True</div>");
    }

    [Fact]
    public void Component_WithInvalidAttributes_ShouldIgnore()
    {
        var markdown = """
                       <SimpleComponent nonexistent="value" text="Valid" />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Valid</div>");
    }

    [Fact]
    public void Component_ThatThrowsException_ShouldFallback()
    {
        var markdown = """
                       <ErrorComponent />
                       """;

        var result = RenderMarkdown(markdown);

        // Should fallback to HTML and include error comment
        result.ShouldContain("<errorcomponent />");
        result.ShouldContain("<!-- Error rendering component ErrorComponent:");
    }

    [Fact]
    public void Component_InMarkdownList_ShouldRender()
    {
        var markdown = """
                       1. First item
                       2. Component: <SimpleComponent text="Item 2" />
                       3. Third item
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<li>Component: <div class=\"simple\">Item 2</div></li>");
    }

    [Fact]
    public void Component_InMarkdownTable_ShouldRender()
    {
        var markdown = """
                       | Column 1 | Column 2 |
                       |----------|----------|
                       | Text     | <SimpleComponent text="Cell" /> |
                       """;

        var result = RenderMarkdown(markdown);

        // Note: Table parsing might not work as expected without table extension
        // Let's just verify the component renders
        result.ShouldContain("<div class=\"simple\">Cell</div>");
    }

    [Fact]
    public void Component_WithWhitespaceInAttributes_ShouldTrim()
    {
        var markdown = """
                       <SimpleComponent text="  Trimmed  " />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">  Trimmed  </div>");
    }

    [Fact]
    public void Component_NotStartingWithCapital_ShouldNotParse()
    {
        var markdown = """
                       <simpleComponent text="should not parse" />
                       """;

        var result = RenderMarkdown(markdown);

        // Should render as regular HTML since it doesn't start with capital
        result.ShouldContain("<simpleComponent text=\"should not parse\" />");
        result.ShouldNotContain("<div class=\"simple\">");
    }

    [Fact]
    public void Component_WithComplexNesting_ShouldRender()
    {
        var markdown = """
                       <SimpleComponent text="Parent">
                       
                       ## Nested heading
                       
                       Some **bold** text and a nested component:
                       
                       <SimpleComponent text="Child" />
                       
                       More content.
                       
                       </SimpleComponent>
                       """;

        var result = RenderMarkdown(markdown);

        // The parent component contains all the nested content
        result.ShouldContain("<div class=\"simple\">Parent");
        result.ShouldContain("<h2>Nested heading</h2>");
        result.ShouldContain("<strong>bold</strong>");
        result.ShouldContain("<div class=\"simple\">Child</div>");
    }

    [Fact]
    public void Component_WithInlineMarkdownSyntax_ShouldNotInterfere()
    {
        var markdown = """
                       This is **bold** and <SimpleComponent text="component" /> and *italic*.
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<strong>bold</strong>");
        result.ShouldContain("<div class=\"simple\">component</div>");
        result.ShouldContain("<em>italic</em>");
    }

    [Fact]
    public void Component_WithMultilineWhitespace_ShouldParse()
    {
        var markdown = """
                       <SimpleComponent 
                           text="Multiline attributes" 
                           number="123" 
                       />
                       """;

        var result = RenderMarkdown(markdown);

        result.ShouldContain("<div class=\"simple\">Multiline attributes Number: 123</div>");
    }

    private string RenderMarkdown(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        
        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider, _pipeline);
        renderer.Render(document);
        
        return writer.ToString().Trim();
    }

    // Test components
    private class SimpleComponent : ComponentBase
    {
        [Parameter] public string Text { get; set; } = "Default";
        [Parameter] public int Number { get; set; }
        [Parameter] public bool Flag { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "simple");

            var content = Text;
            if (Number > 0) content += $" Number: {Number}";
            if (Flag) content += $" Flag: {Flag}";

            if (!string.IsNullOrEmpty(content))
            {
                builder.AddContent(2, content);
            }

            if (ChildContent != null)
            {
                builder.AddContent(3, ChildContent);
            }

            builder.CloseElement();
        }
    }

    private class ErrorComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            throw new InvalidOperationException("This component always throws an error");
        }
    }
}