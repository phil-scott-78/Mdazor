using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using BlazorHtmlRenderer = Microsoft.AspNetCore.Components.Web.HtmlRenderer;
using MarkdigHtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace Mdazor;

public class BlazorRenderer : MarkdigHtmlRenderer
{
    private readonly IComponentRegistry _componentRegistry;
    private readonly IServiceProvider _serviceProvider;

    public BlazorRenderer(TextWriter writer, IComponentRegistry componentRegistry, IServiceProvider serviceProvider) 
        : base(writer)
    {
        _componentRegistry = componentRegistry;
        _serviceProvider = serviceProvider;
        
        ObjectRenderers.Add(new ComponentBlockRenderer());
        ObjectRenderers.Add(new ComponentInlineRenderer());
    }

    private class ComponentBlockRenderer : HtmlObjectRenderer<ComponentBlock>
    {
        protected override void Write(MarkdigHtmlRenderer renderer, ComponentBlock block)
        {
            if (renderer is not BlazorRenderer blazorRenderer)
            {
                return;
            }

            blazorRenderer.WriteComponentBlock(block);
        }
    }

    private class ComponentInlineRenderer : HtmlObjectRenderer<ComponentInline>
    {
        protected override void Write(MarkdigHtmlRenderer renderer, ComponentInline inline)
        {
            if (renderer is not BlazorRenderer blazorRenderer)
            {
                return;
            }

            blazorRenderer.WriteComponentInline(inline);
        }
    }

    private void WriteComponentBlock(ComponentBlock block)
    {
        var componentType = _componentRegistry.GetComponentType(block.ComponentName);
        if (componentType == null)
        {
            // Render as normal HTML tag
            WriteComponentAsHtml(block);
            return;
        }

        try
        {
            var html = RenderComponentAsync(componentType, block.Attributes, block).GetAwaiter().GetResult();
            Write(html);
        }
        catch (Exception ex)
        {
            WriteComponentAsHtml(block);
            Write($"<!-- Error rendering component {block.ComponentName}: {ex.Message} -->");
        }
    }

    private void WriteComponentInline(ComponentInline inline)
    {
        var componentType = _componentRegistry.GetComponentType(inline.ComponentName);
        if (componentType == null)
        {
            // Render as normal HTML tag
            WriteComponentAsHtml(inline);
            return;
        }

        try
        {
            var html = RenderComponentAsync(componentType, inline.Attributes, null).GetAwaiter().GetResult();
            Write(html);
        }
        catch (Exception ex)
        {
            WriteComponentAsHtml(inline);
            Write($"<!-- Error rendering component {inline.ComponentName}: {ex.Message} -->");
        }
    }

    private async Task<string> RenderComponentAsync(Type componentType, Dictionary<string, string> attributes, ComponentBlock? block)
    {
        using var scope = _serviceProvider.CreateScope();
        var htmlRenderer = scope.ServiceProvider.GetRequiredService<BlazorHtmlRenderer>();

        var parameters = new Dictionary<string, object?>();
        
        // Convert string attributes to proper types with case-insensitive matching
        foreach (var attr in attributes)
        {
            var property = componentType.GetProperty(attr.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.CanWrite)
            {
                var value = ConvertAttributeValue(attr.Value, property.PropertyType);
                parameters[property.Name] = value; // Use the actual property name, not the attribute name
            }
        }

        // Handle child content for block components
        if (block is { IsSelfClosing: false, Count: > 0 })
        {
            var childContentProperty = componentType.GetProperty("ChildContent");
            if (childContentProperty != null)
            {
                var childMarkdown = RenderChildContent(block);
                var pipeline = new MarkdownPipelineBuilder().Use<MdazorExtension>().Build();
                var childHtml = Markdown.ToHtml(childMarkdown, pipeline);
                
                parameters["ChildContent"] = new RenderFragment(builder =>
                {
                    builder.AddMarkupContent(0, childHtml);
                });
            }
        }

        var parameterView = ParameterView.FromDictionary(parameters);
        return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await htmlRenderer.RenderComponentAsync(componentType, parameterView);
            return output.ToHtmlString();
        });
    }

    private string RenderChildContent(ComponentBlock block)
    {
        using var writer = new StringWriter();
        var childRenderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider);
        
        foreach (var child in block)
        {
            childRenderer.Render(child);
        }
        
        return writer.ToString();
    }

    private void WriteComponentAsHtml(ComponentBlock block)
    {
        Write($"<{block.ComponentName.ToLowerInvariant()}");
        WriteAttributes(block.Attributes);
        
        if (block.IsSelfClosing)
        {
            Write(" />");
        }
        else
        {
            Write(">");
            
            // Render child content
            if (block.Count > 0)
            {
                var childContent = RenderChildContent(block);
                Write(childContent);
            }
            
            Write($"</{block.ComponentName.ToLowerInvariant()}>");
        }
    }

    private void WriteComponentAsHtml(ComponentInline inline)
    {
        Write($"<{inline.ComponentName.ToLowerInvariant()}");
        WriteAttributes(inline.Attributes);
        Write(" />");
    }

    private void WriteAttributes(Dictionary<string, string> attributes)
    {
        foreach (var attr in attributes)
        {
            Write($" {attr.Key}=\"{HttpUtility.HtmlEncode(attr.Value)}\"");
        }
    }

    private static object? ConvertAttributeValue(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int) && int.TryParse(value, out var intValue)) return intValue;
        if (targetType == typeof(bool) && bool.TryParse(value, out var boolValue)) return boolValue;
        if (targetType == typeof(double) && double.TryParse(value, out var doubleValue)) return doubleValue;

        return value;
    }
}