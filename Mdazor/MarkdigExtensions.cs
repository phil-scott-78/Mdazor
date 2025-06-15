using Markdig;
using Markdig.Extensions.CustomContainers;

namespace Mdazor;

public static class MarkdigExtensions
{
    /// <summary>
    /// Registers the Mdazor extension with the Markdig pipeline.
    /// </summary>
    /// <param name="pipeline">The Markdig pipeline.</param>
    /// <returns>The Markdig pipeline with the Mdazor extension added.</returns>
    public static MarkdownPipelineBuilder UseMdazor(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready<MdazorExtension>();
        return pipeline;
    }
}