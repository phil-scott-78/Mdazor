using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class IndentedNestedComponentTests
{
    private readonly MarkdownPipeline _pipeline;

    public IndentedNestedComponentTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMdazor()
            .AddMdazorComponent<CardGrid>()
            .AddMdazorComponent<LinkCard>();

        var serviceProvider = services.BuildServiceProvider();
        _pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .Build();
    }

    [Fact]
    public void Nested_component_indented_two_spaces_should_keep_its_child_content()
    {
        // Reproduces the user-reported bug:
        // <LinkCard> tags rendered, but ChildContent silently stripped because
        // the inner text sits at column 4 (CommonMark's indented-code-block territory).
        var markdown = """
                       <CardGrid Columns="3">
                         <LinkCard Title="Our schedule" Href="/schedule/">
                           Six days, four teachers, twenty-five classes.
                         </LinkCard>
                       </CardGrid>
                       """;

        var result = Markdown.ToHtml(markdown, _pipeline);

        result.ShouldContain("class=\"card-grid\"");
        result.ShouldContain("class=\"link-card\"");
        result.ShouldContain("Our schedule");
        result.ShouldContain("Six days, four teachers, twenty-five classes.");
        result.ShouldNotContain("<pre>");
        result.ShouldNotContain("<code>");
    }

    [Fact]
    public void Nested_component_unindented_keeps_its_child_content()
    {
        // Sanity check: the un-indented form (which the README shows) works.
        var markdown = """
                       <CardGrid Columns="3">
                       <LinkCard Title="Our schedule" Href="/schedule/">
                       Six days, four teachers, twenty-five classes.
                       </LinkCard>
                       </CardGrid>
                       """;

        var result = Markdown.ToHtml(markdown, _pipeline);

        result.ShouldContain("class=\"card-grid\"");
        result.ShouldContain("class=\"link-card\"");
        result.ShouldContain("Our schedule");
        result.ShouldContain("Six days, four teachers, twenty-five classes.");
    }

    private class CardGrid : ComponentBase
    {
        [Parameter] public int Columns { get; set; } = 1;
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "card-grid");
            builder.AddAttribute(2, "data-columns", Columns.ToString());
            if (ChildContent != null)
            {
                builder.AddContent(3, ChildContent);
            }
            builder.CloseElement();
        }
    }

    private class LinkCard : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "";
        [Parameter] public string Href { get; set; } = "";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "class", "link-card");
            builder.AddAttribute(2, "href", Href);

            builder.OpenElement(3, "h3");
            builder.AddContent(4, Title);
            builder.CloseElement();

            if (ChildContent != null)
            {
                builder.OpenElement(5, "div");
                builder.AddAttribute(6, "class", "link-card-body");
                builder.AddContent(7, ChildContent);
                builder.CloseElement();
            }

            builder.CloseElement();
        }
    }
}
