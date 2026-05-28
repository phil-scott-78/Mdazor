using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;

namespace Mdazor;

public interface IComponentRegistry
{
    void RegisterComponent<T>() where T : ComponentBase;
    void RegisterComponent<T>(string name) where T : ComponentBase;
    void RegisterComponent<T>(IComponentRenderMode? renderMode) where T : ComponentBase;
    void RegisterComponent<T>(string name, IComponentRenderMode? renderMode) where T : ComponentBase;
    Type? GetComponentType(string name);
    IComponentRenderMode? GetRenderMode(string name);
    bool IsRegistered(string name);
}

public class ComponentRegistry : IComponentRegistry
{
    private readonly ConcurrentDictionary<string, (Type Type, IComponentRenderMode? RenderMode)> _components = new();

    public void RegisterComponent<T>() where T : ComponentBase
        => _components.TryAdd(typeof(T).Name, (typeof(T), null));

    public void RegisterComponent<T>(string name) where T : ComponentBase
        => _components.TryAdd(name, (typeof(T), null));

    public void RegisterComponent<T>(IComponentRenderMode? renderMode) where T : ComponentBase
        => _components.TryAdd(typeof(T).Name, (typeof(T), renderMode));

    public void RegisterComponent<T>(string name, IComponentRenderMode? renderMode) where T : ComponentBase
        => _components.TryAdd(name, (typeof(T), renderMode));

    public Type? GetComponentType(string name)
        => _components.TryGetValue(name, out var entry) ? entry.Type : null;

    public IComponentRenderMode? GetRenderMode(string name)
        => _components.TryGetValue(name, out var entry) ? entry.RenderMode : null;

    public bool IsRegistered(string name)
        => _components.ContainsKey(name);
}
