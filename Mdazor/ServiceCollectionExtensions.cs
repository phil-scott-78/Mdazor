using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Mdazor;

public static class ServiceCollectionExtensions
{
    private class ComponentRegistrationsHolder
    {
        public List<Action<IComponentRegistry>> Registrations { get; } = new();
    }

    public static IServiceCollection AddMdazor(this IServiceCollection services)
    {
        // Register the component registrations holder as singleton if not already registered
        if (!services.Any(x => x.ServiceType == typeof(ComponentRegistrationsHolder)))
        {
            services.AddSingleton<ComponentRegistrationsHolder>();
        }

        services.AddSingleton<IComponentRegistry>(serviceProvider =>
        {
            var registry = new ComponentRegistry();
            var holder = serviceProvider.GetRequiredService<ComponentRegistrationsHolder>();
            
            foreach (var registration in holder.Registrations)
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
        services.AddComponentRegistration(registry => registry.RegisterComponent<T>());
        return services;
    }

    public static IServiceCollection AddMdazorComponent<T>(this IServiceCollection services, string name) where T : ComponentBase
    {
        services.AddTransient<T>();
        services.AddComponentRegistration(registry => registry.RegisterComponent<T>(name));
        return services;
    }

    private static void AddComponentRegistration(this IServiceCollection services, Action<IComponentRegistry> registration)
    {
        // Ensure the holder is registered
        if (!services.Any(x => x.ServiceType == typeof(ComponentRegistrationsHolder)))
        {
            services.AddSingleton<ComponentRegistrationsHolder>();
        }

        // Find the holder service descriptor and add to its registrations
        var holderDescriptor = services.First(x => x.ServiceType == typeof(ComponentRegistrationsHolder));
        if (holderDescriptor.ImplementationInstance is ComponentRegistrationsHolder holder)
        {
            holder.Registrations.Add(registration);
        }
        else
        {
            // If not an instance, we need to create a new descriptor with an instance
            services.Remove(holderDescriptor);
            var newHolder = new ComponentRegistrationsHolder();
            newHolder.Registrations.Add(registration);
            services.AddSingleton(newHolder);
        }
    }
}