using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Mdazor;

public static class ServiceCollectionExtensions
{
    private static readonly List<Action<IComponentRegistry>> ComponentRegistrations = new();

    public static IServiceCollection AddMdazor(this IServiceCollection services)
    {
        services.AddSingleton<IComponentRegistry>(_ =>
        {
            var registry = new ComponentRegistry();
            foreach (var registration in ComponentRegistrations)
            {
                registration(registry);
            }
            return registry;
        });

        // critical this is transient, otherwise you'll get issues like DI throwing
        // Cannot resolve scoped service 'Microsoft.AspNetCore.Components.ICascadingValueSupplier' from the root provider.
        services.AddTransient<HtmlRenderer>();
        
        // Register a pre-configured MarkdownPipeline that supports components
        services.AddSingleton<MarkdownPipeline>(serviceProvider =>
        {
            return new MarkdownPipelineBuilder()
                .UseMdazor(serviceProvider)
                .Build();
        });

        return services;
    }

    public static IServiceCollection AddMdazorComponent<T>(this IServiceCollection services) where T : ComponentBase
    {
        services.AddTransient<T>();
        ComponentRegistrations.Add(registry => registry.RegisterComponent<T>());
        return services;
    }

    public static IServiceCollection AddMdazorComponent<T>(this IServiceCollection services, string name) where T : ComponentBase
    {
        services.AddTransient<T>();
        ComponentRegistrations.Add(registry => registry.RegisterComponent<T>(name));
        return services;
    }
}