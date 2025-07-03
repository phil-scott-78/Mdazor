using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Markdig;
using Xunit;

namespace Mdazor.Tests;

public class TestCard : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public string? Title { get; set; }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "test-card");
        
        if (Icon != null)
        {
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "class", "icon");
            builder.AddContent(4, Icon);
            builder.CloseElement();
        }
        
        if (!string.IsNullOrEmpty(Title))
        {
            builder.OpenElement(5, "h2");
            builder.AddContent(6, Title);
            builder.CloseElement();
        }
        
        if (ChildContent != null)
        {
            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "class", "content");
            builder.AddContent(9, ChildContent);
            builder.CloseElement();
        }
        
        builder.CloseElement();
    }
}

public class IconRenderFragmentTests
{
    [Fact]
    public void Card_WithIconParameter_RendersIconContent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMdazor()
            .AddMdazorComponent<TestCard>();
            
        services.AddSingleton<HtmlRenderer>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .Build();

        var markdown = @"
<TestCard Title=""My Test Card"">
<Icon>
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" width=""2em"" height=""2em"">
    <path d=""M10 19.5C6.22876 19.5 4.34315 19.5 3.17157 18.3284C2 17.1569 2 15.2712 2 11.5V10.5C2 6.72876 2 4.84315 3.17157 3.67157C4.34315 2.5 6.22876 2.5 10 2.5H13.5C16.7875 2.5 18.4312 2.5 19.5376 3.40796C19.7401 3.57418 19.9258 3.75989 20.092 3.96243C21 5.06878 21 6.71252 21 10"" stroke-width=""1.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
</svg>
</Icon>
This is the card content.
</TestCard>";

        // Act
        var document = Markdown.Parse(markdown, pipeline);
        var html = document.ToHtml(pipeline);

        // Debug: Output the HTML to see what's being generated
        Console.WriteLine($"Generated HTML: {html}");
        
        // Assert
        Assert.Contains(@"<svg xmlns=""http://www.w3.org/2000/svg""", html);
        Assert.Contains(@"My Test Card", html);
        Assert.Contains(@"This is the card content.", html);
        Assert.DoesNotContain(@"<icon>", html); // Should not contain literal icon tag
        Assert.Contains(@"class=""icon""", html); // Should contain the icon wrapper
    }
}