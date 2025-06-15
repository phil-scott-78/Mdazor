using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Mdazor.Tests;

public class BlazorRendererTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IComponentRegistry _componentRegistry;
    private readonly MarkdownPipeline _pipeline;

    public BlazorRendererTests()
    {
        var services = new ServiceCollection();
        services.AddMdazor();
        services.AddSingleton<MarkdownPipeline>(new MarkdownPipelineBuilder()
            .UseMdazor()
            .Build());
        _serviceProvider = services.BuildServiceProvider();
        _componentRegistry = _serviceProvider.GetRequiredService<IComponentRegistry>();
        _pipeline = _serviceProvider.GetRequiredService<MarkdownPipeline>();
    }

    [Fact]
    public void RenderUnknownComponent_ShouldRenderAsHtml()
    {
        var markdown = "<UnknownComponent attr=\"value\" />";
        var document = Markdown.Parse(markdown, _pipeline);

        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider);
        renderer.Render(document);
        var result = writer.ToString();

        result.ShouldContain("<unknowncomponent attr=\"value\" />");
    }

    [Fact]
    public void RenderUnknownComponentWithContent_ShouldRenderAsHtml()
    {
        var markdown = @"<UnknownComponent title=""test"">
Some content
</UnknownComponent>";
        var document = Markdown.Parse(markdown, _pipeline);

        using var writer = new StringWriter();
        var renderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider);
        renderer.Render(document);
        var result = writer.ToString();

        result.ShouldContain("<unknowncomponent title=\"test\">");
        result.ShouldContain("Some content");
        result.ShouldContain("</unknowncomponent>");
    }
}