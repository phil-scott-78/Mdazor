using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Web;
using BlazorHtmlRenderer = Microsoft.AspNetCore.Components.Web.HtmlRenderer;
using MarkdigHtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace Mdazor;

public class BlazorRenderer : MarkdigHtmlRenderer
{
    private readonly IComponentRegistry _componentRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly MarkdownPipeline _pipeline;

    public BlazorRenderer(TextWriter writer, IComponentRegistry componentRegistry, IServiceProvider serviceProvider, MarkdownPipeline pipeline) 
        : base(writer)
    {
        _componentRegistry = componentRegistry;
        _serviceProvider = serviceProvider;
        _pipeline = pipeline;

        ObjectRenderers.Add(new ComponentBlockRenderer());
        ObjectRenderers.Add(new ComponentInlineRenderer());
    }

    public BlazorRenderer(TextWriter writer, IComponentRegistry componentRegistry, IServiceProvider serviceProvider, MarkdownPipeline pipeline, MarkdigHtmlRenderer sourceRenderer) 
        : base(writer)
    {
        _componentRegistry = componentRegistry;
        _serviceProvider = serviceProvider;
        _pipeline = pipeline;

        _pipeline.Setup(this);

    }

    private class ComponentBlockRenderer : HtmlObjectRenderer<ComponentBlock>
    {
        protected override void Write(MarkdigHtmlRenderer renderer, ComponentBlock block)
        {
            if (renderer is not BlazorRenderer blazorRenderer)
            {
                return;
            }

            blazorRenderer.WriteComponentBlock(block, renderer);
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

            blazorRenderer.WriteComponentInline(inline, renderer);
        }
    }

    private void WriteComponentBlock(ComponentBlock block, MarkdigHtmlRenderer sourceRenderer)
    {
        var componentType = _componentRegistry.GetComponentType(block.ComponentName);
        if (componentType == null)
        {
            // Render as normal HTML tag
            WriteComponentAsHtml(block, sourceRenderer);
            return;
        }

        try
        {
            var html = RenderComponentAsync(componentType, block.Attributes, block, sourceRenderer).GetAwaiter().GetResult();
            Write(html);
        }
        catch (Exception ex)
        {
            WriteComponentAsHtml(block, sourceRenderer);
            Write($"<!-- Error rendering component {block.ComponentName}: {ex.Message} -->");
        }
    }

    private void WriteComponentInline(ComponentInline inline, MarkdigHtmlRenderer sourceRenderer)
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
            var html = RenderComponentAsync(componentType, inline.Attributes, null, sourceRenderer).GetAwaiter().GetResult();
            Write(html);
        }
        catch (Exception ex)
        {
            WriteComponentAsHtml(inline);
            Write($"<!-- Error rendering component {inline.ComponentName}: {ex.Message} -->");
        }
    }

    private async Task<string> RenderComponentAsync(Type componentType, Dictionary<string, string> attributes, ComponentBlock? block, MarkdigHtmlRenderer sourceRenderer)
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
            // Get all RenderFragment properties from the component
            var renderFragmentProperties = componentType.GetProperties()
                .Where(p => p.PropertyType == typeof(RenderFragment) || 
                           (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                            Nullable.GetUnderlyingType(p.PropertyType) == typeof(RenderFragment)))
                .ToList();
            
            var namedRenderFragments = new Dictionary<string, List<Block>>();
            var childContentBlocks = new List<Block>();
            
            // Parse child blocks to identify named RenderFragment content
            foreach (var child in block)
            {
                if (child is ComponentBlock childComponent)
                {
                    var matchingProperty = renderFragmentProperties.FirstOrDefault(p => 
                        string.Equals(p.Name, childComponent.ComponentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingProperty != null)
                    {
                        if (!namedRenderFragments.ContainsKey(matchingProperty.Name))
                        {
                            namedRenderFragments[matchingProperty.Name] = new List<Block>();
                        }
                        
                        // Add the content inside the named RenderFragment element
                        namedRenderFragments[matchingProperty.Name].AddRange(childComponent);
                        continue;
                    }
                }
                
                // Add to regular child content if not a named RenderFragment
                childContentBlocks.Add(child);
            }
            
            // Create RenderFragment parameters for named fragments
            foreach (var kvp in namedRenderFragments)
            {
                var propertyName = kvp.Key;
                var blocks = kvp.Value;
                
                if (blocks.Count > 0)
                {
                    var html = RenderBlocks(blocks, sourceRenderer);
                    parameters[propertyName] = new RenderFragment(builder =>
                    {
                        builder.AddMarkupContent(0, html);
                    });
                }
            }
            
            // Handle remaining content as ChildContent
            var childContentProperty = componentType.GetProperty("ChildContent");
            if (childContentProperty != null && childContentBlocks.Count > 0)
            {
                var childHtml = RenderBlocks(childContentBlocks, sourceRenderer);
                
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

    private string RenderChildContent(ComponentBlock block, MarkdigHtmlRenderer sourceRenderer)
    {
        using var writer = new StringWriter();
        var childRenderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider, _pipeline, sourceRenderer);
        
        foreach (var child in block)
        {
            childRenderer.Render(child);
        }
        
        return writer.ToString();
    }
    
    private string RenderBlocks(IEnumerable<Block> blocks, MarkdigHtmlRenderer sourceRenderer)
    {
        using var writer = new StringWriter();
        var childRenderer = new BlazorRenderer(writer, _componentRegistry, _serviceProvider, _pipeline, sourceRenderer);
        
        foreach (var block in blocks)
        {
            childRenderer.Render(block);
        }
        
        return writer.ToString();
    }

    private void WriteComponentAsHtml(ComponentBlock block, MarkdigHtmlRenderer sourceRenderer)
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
                var childContent = RenderChildContent(block, sourceRenderer);
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