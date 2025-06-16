using Markdig;
using Microsoft.Extensions.DependencyInjection;

namespace Mdazor;

public static class MarkdigExtensions
{
    /// <summary>
    /// Registers the Mdazor extension with the Markdig pipeline with full component rendering support.
    /// This enables using Markdown.ToHtml() with Blazor components.
    /// </summary>
    /// <param name="pipeline">The Markdig pipeline.</param>
    /// <param name="serviceProvider">The service provider containing component registry and services.</param>
    /// <returns>The Markdig pipeline with the Mdazor extension added.</returns>
    public static MarkdownPipelineBuilder UseMdazor(this MarkdownPipelineBuilder pipeline, IServiceProvider serviceProvider)
    {
        var componentRegistry = serviceProvider.GetRequiredService<IComponentRegistry>();
        pipeline.Extensions.AddIfNotAlready(new MdazorExtension(componentRegistry, serviceProvider));
        return pipeline;
    }
}