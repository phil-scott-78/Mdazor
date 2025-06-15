using Markdig;
using Markdig.Renderers;

namespace Mdazor;

public class MdazorExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<ComponentBlockParser>())
        {
            pipeline.BlockParsers.Insert(0, new ComponentBlockParser());
        }

        if (!pipeline.InlineParsers.Contains<ComponentInlineParser>())
        {
            pipeline.InlineParsers.Insert(0, new ComponentInlineParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        // Renderer setup is handled by BlazorRenderer
    }
}