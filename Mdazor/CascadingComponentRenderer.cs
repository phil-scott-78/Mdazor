using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorHtmlRenderer = Microsoft.AspNetCore.Components.Web.HtmlRenderer;

namespace Mdazor;

// Renders a Blazor component to an HTML string, optionally wrapping it in a
// CascadingValue<MdazorContext> so the component (and its descendants) can read
// host-supplied context via [CascadingParameter] MdazorContext.
//
// CascadingValue emits no markup of its own, so wrapping never changes the HTML —
// it only makes the cascade visible to the component. When there is no context the
// component is rendered directly, byte-for-byte as before.
internal static class CascadingComponentRenderer
{
    public static Task<string> RenderToHtmlAsync(
        BlazorHtmlRenderer htmlRenderer,
        Type componentType,
        IDictionary<string, object?> parameters,
        MdazorContext? context)
    {
        return htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            Type rootType;
            ParameterView rootParameters;

            if (context is null)
            {
                rootType = componentType;
                rootParameters = ParameterView.FromDictionary(parameters);
            }
            else
            {
                rootType = typeof(CascadingValue<MdazorContext>);
                rootParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Value"] = context,
                    ["IsFixed"] = true,
                    ["ChildContent"] = (RenderFragment)(builder => BuildComponent(builder, componentType, parameters)),
                });
            }

            var output = await htmlRenderer.RenderComponentAsync(rootType, rootParameters);
            return output.ToHtmlString();
        });
    }

    private static void BuildComponent(RenderTreeBuilder builder, Type componentType, IDictionary<string, object?> parameters)
    {
        builder.OpenComponent(0, componentType);
        var seq = 1;
        foreach (var parameter in parameters)
        {
            builder.AddComponentParameter(seq++, parameter.Key, parameter.Value);
        }
        builder.CloseComponent();
    }
}
