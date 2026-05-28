using Markdig;
using Markdig.Syntax;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Reflection;
using System.Text.RegularExpressions;
using MarkdigHtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace Mdazor;

internal static class MarkdownRenderTreeWriter
{
    private static readonly Regex SentinelRegex = new(@"(\d+)", RegexOptions.Compiled);

    public static void Write(RenderTreeBuilder builder, MarkdownDocument doc, IComponentRegistry registry, MarkdownPipeline pipeline)
    {
        int seq = 0;
        foreach (var block in doc)
        {
            seq = RenderBlock(builder, block, seq, registry, pipeline);
        }
    }

    private static int RenderBlock(RenderTreeBuilder b, Block block, int seq, IComponentRegistry registry, MarkdownPipeline pipeline)
    {
        return block switch
        {
            ComponentBlock cb => EmitComponentBlock(b, seq, cb, registry, pipeline),
            ParagraphBlock pb when ContainsComponentInline(pb) => EmitParagraphWithInlines(b, seq, pb, registry, pipeline),
            _ => EmitStaticHtml(b, seq, block, pipeline),
        };
    }

    private static int EmitStaticHtml(RenderTreeBuilder b, int seq, Block block, MarkdownPipeline pipeline)
    {
        var html = RenderBlockToHtml(block, pipeline);
        if (html.Length > 0)
            b.AddMarkupContent(seq++, html);
        return seq;
    }

    private static string RenderBlockToHtml(Block block, MarkdownPipeline pipeline)
    {
        using var sw = new StringWriter();
        var hr = new MarkdigHtmlRenderer(sw);
        pipeline.Setup(hr);
        hr.Render(block);
        return sw.ToString();
    }

    private static int EmitComponentBlock(RenderTreeBuilder b, int seq, ComponentBlock block, IComponentRegistry registry, MarkdownPipeline pipeline)
    {
        var type = registry.GetComponentType(block.ComponentName);
        if (type is null)
        {
            // Unknown component: fall through to Mdazor's static path which emits the raw HTML tag.
            return EmitStaticHtml(b, seq, block, pipeline);
        }

        b.OpenComponent(seq++, type);

        string? key = ExtractKey(block.Attributes, out var attrs);
        seq = WriteAttributeParameters(b, seq, type, attrs);

        if (!block.IsSelfClosing && block.Count > 0)
        {
            seq = WriteChildFragments(b, seq, type, block, registry, pipeline);
        }

        var renderMode = registry.GetRenderMode(block.ComponentName);
        if (renderMode is not null) b.AddComponentRenderMode(renderMode);
        if (key is not null) b.SetKey(key);

        b.CloseComponent();
        return seq;
    }

    private static int EmitComponentInline(RenderTreeBuilder b, int seq, ComponentInline inline, IComponentRegistry registry)
    {
        var type = registry.GetComponentType(inline.ComponentName);
        if (type is null)
        {
            b.AddMarkupContent(seq++, $"<{inline.ComponentName.ToLowerInvariant()} />");
            return seq;
        }

        b.OpenComponent(seq++, type);

        string? key = ExtractKey(inline.Attributes, out var attrs);
        seq = WriteAttributeParameters(b, seq, type, attrs);

        var renderMode = registry.GetRenderMode(inline.ComponentName);
        if (renderMode is not null) b.AddComponentRenderMode(renderMode);
        if (key is not null) b.SetKey(key);

        b.CloseComponent();
        return seq;
    }

    private static int EmitParagraphWithInlines(RenderTreeBuilder b, int seq, ParagraphBlock paragraph, IComponentRegistry registry, MarkdownPipeline pipeline)
    {
        var placeholders = new List<ComponentInline>();
        using var sw = new StringWriter();
        var hr = new MarkdigHtmlRenderer(sw);
        pipeline.Setup(hr);

        // Replace any ComponentInlineRenderer Markdig set up with our placeholder emitter,
        // so each ComponentInline becomes a sentinel token we can split on.
        for (int i = hr.ObjectRenderers.Count - 1; i >= 0; i--)
        {
            if (hr.ObjectRenderers[i] is ComponentInlineRenderer)
                hr.ObjectRenderers.RemoveAt(i);
        }
        hr.ObjectRenderers.Add(new PlaceholderInlineRenderer(placeholders));

        hr.Render(paragraph);
        var html = sw.ToString();

        var parts = SentinelRegex.Split(html);
        for (int i = 0; i < parts.Length; i++)
        {
            if ((i & 1) == 0)
            {
                if (parts[i].Length > 0)
                    b.AddMarkupContent(seq++, parts[i]);
            }
            else
            {
                var idx = int.Parse(parts[i]);
                seq = EmitComponentInline(b, seq, placeholders[idx], registry);
            }
        }
        return seq;
    }

    private static int WriteAttributeParameters(RenderTreeBuilder b, int seq, Type componentType, IEnumerable<KeyValuePair<string, string>> attributes)
    {
        foreach (var attr in attributes)
        {
            var prop = componentType.GetProperty(attr.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop?.CanWrite == true)
            {
                b.AddComponentParameter(seq++, prop.Name, AttributeConverter.Convert(attr.Value, prop.PropertyType));
            }
        }
        return seq;
    }

    private static int WriteChildFragments(RenderTreeBuilder b, int seq, Type componentType, ComponentBlock block, IComponentRegistry registry, MarkdownPipeline pipeline)
    {
        var split = ChildContentSplitter.Split(componentType, block);

        RenderFragment Fragment(List<Block> blocks) => childBuilder =>
        {
            int s = 0;
            foreach (var inner in blocks)
                s = RenderBlock(childBuilder, inner, s, registry, pipeline);
        };

        foreach (var (propertyName, blocks) in split.NamedFragments)
        {
            if (blocks.Count == 0) continue;
            b.AddComponentParameter(seq++, propertyName, Fragment(blocks));
        }

        if (componentType.GetProperty("ChildContent") != null && split.DefaultBlocks.Count > 0)
        {
            b.AddComponentParameter(seq++, "ChildContent", Fragment(split.DefaultBlocks));
        }

        return seq;
    }

    private static string? ExtractKey(Dictionary<string, string> source, out Dictionary<string, string> remaining)
    {
        string? key = null;
        remaining = new Dictionary<string, string>(source.Count);
        foreach (var kvp in source)
        {
            if (string.Equals(kvp.Key, "key", StringComparison.OrdinalIgnoreCase))
                key = kvp.Value;
            else
                remaining[kvp.Key] = kvp.Value;
        }
        return key;
    }

    private static bool ContainsComponentInline(ParagraphBlock paragraph)
    {
        if (paragraph.Inline is null) return false;
        foreach (var descendant in paragraph.Inline.Descendants())
        {
            if (descendant is ComponentInline) return true;
        }
        return false;
    }
}
