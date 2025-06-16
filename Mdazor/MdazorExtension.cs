using Markdig;
using Markdig.Renderers;

namespace Mdazor;

public class MdazorExtension : IMarkdownExtension
{
    private readonly IComponentRegistry _componentRegistry;
    private readonly IServiceProvider _serviceProvider;

    public MdazorExtension(IComponentRegistry componentRegistry, IServiceProvider serviceProvider)
    {
        _componentRegistry = componentRegistry;
        _serviceProvider = serviceProvider;
    }

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
        if (renderer is HtmlRenderer htmlRenderer)
        {
            // Replace the HtmlRenderer with our BlazorRenderer
            // Note: This is a bit of a hack since we can't directly replace the renderer
            // We'll add the component renderers to the existing HtmlRenderer
            if (!htmlRenderer.ObjectRenderers.Contains<ComponentBlockRenderer>())
            {
                htmlRenderer.ObjectRenderers.Add(new ComponentBlockRenderer(_componentRegistry, _serviceProvider, pipeline));
            }
            if (!htmlRenderer.ObjectRenderers.Contains<ComponentInlineRenderer>())
            {
                htmlRenderer.ObjectRenderers.Add(new ComponentInlineRenderer(_componentRegistry, _serviceProvider));
            }
        }
    }
}