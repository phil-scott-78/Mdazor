using Markdig.Renderers.Html;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Web;
using BlazorHtmlRenderer = Microsoft.AspNetCore.Components.Web.HtmlRenderer;
using MarkdigHtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace Mdazor;

public class ComponentInlineRenderer : HtmlObjectRenderer<ComponentInline>
{
    private readonly IComponentRegistry _componentRegistry;
    private readonly IServiceProvider _serviceProvider;

    public ComponentInlineRenderer(IComponentRegistry componentRegistry, IServiceProvider serviceProvider)
    {
        _componentRegistry = componentRegistry;
        _serviceProvider = serviceProvider;
    }

    protected override void Write(MarkdigHtmlRenderer renderer, ComponentInline inline)
    {
        var componentType = _componentRegistry.GetComponentType(inline.ComponentName);
        if (componentType == null)
        {
            // Render as normal HTML tag
            WriteComponentAsHtml(renderer, inline);
            return;
        }

        try
        {
            var html = AsyncHelper.RunSync(() => RenderComponentAsync(componentType, inline.Attributes));
            renderer.Write(html);
        }
        catch (Exception ex)
        {
            WriteComponentAsHtml(renderer, inline);
            renderer.Write($"<!-- Error rendering component {inline.ComponentName}: {ex.Message} -->");
        }
    }

    private async Task<string> RenderComponentAsync(Type componentType, Dictionary<string, string> attributes)
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

        var parameterView = ParameterView.FromDictionary(parameters);
        return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await htmlRenderer.RenderComponentAsync(componentType, parameterView);
            return output.ToHtmlString();
        });
    }

    private void WriteComponentAsHtml(MarkdigHtmlRenderer renderer, ComponentInline inline)
    {
        renderer.Write($"<{inline.ComponentName.ToLowerInvariant()}");
        WriteAttributes(renderer, inline.Attributes);
        renderer.Write(" />");
    }

    private void WriteAttributes(MarkdigHtmlRenderer renderer, Dictionary<string, string> attributes)
    {
        foreach (var attr in attributes)
        {
            renderer.Write($" {attr.Key}=\"{HttpUtility.HtmlEncode(attr.Value)}\"");
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