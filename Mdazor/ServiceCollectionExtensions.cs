using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Mdazor;

public static class ServiceCollectionExtensions
{
    private static readonly List<Action<IComponentRegistry>> _componentRegistrations = new();

    public static IServiceCollection AddMdazor(this IServiceCollection services)
    {
        services.AddSingleton<IComponentRegistry>(provider =>
        {
            var registry = new ComponentRegistry();
            foreach (var registration in _componentRegistrations)
            {
                registration(registry);
            }
            return registry;
        });

        // critical this is transient, otherwise you'll get issues like DI throwing
        // Cannot resolve scoped service 'Microsoft.AspNetCore.Components.ICascadingValueSupplier' from the root provider.
        services.AddTransient<HtmlRenderer>();
        


        return services;
    }

    public static IServiceCollection AddMdazorComponent<T>(this IServiceCollection services) where T : ComponentBase
    {
        services.AddTransient<T>();
        _componentRegistrations.Add(registry => registry.RegisterComponent<T>());
        return services;
    }

    public static IServiceCollection AddMdazorComponent<T>(this IServiceCollection services, string name) where T : ComponentBase
    {
        services.AddTransient<T>();
        _componentRegistrations.Add(registry => registry.RegisterComponent<T>(name));
        return services;
    }
}