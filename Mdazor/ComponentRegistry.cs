using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;

namespace Mdazor;

public interface IComponentRegistry
{
    void RegisterComponent<T>() where T : ComponentBase;
    void RegisterComponent<T>(string name) where T : ComponentBase;
    Type? GetComponentType(string name);
    bool IsRegistered(string name);
}

public class ComponentRegistry : IComponentRegistry
{
    private readonly ConcurrentDictionary<string, Type> _components = new();

    public void RegisterComponent<T>() where T : ComponentBase
    {
        var name = typeof(T).Name;
        _components.TryAdd(name, typeof(T));
    }

    public void RegisterComponent<T>(string name) where T : ComponentBase
    {
        _components.TryAdd(name, typeof(T));
    }

    public Type? GetComponentType(string name)
    {
        return _components.GetValueOrDefault(name);
    }

    public bool IsRegistered(string name)
    {
        return _components.ContainsKey(name);
    }
}