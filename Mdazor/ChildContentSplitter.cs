using Markdig.Syntax;
using Microsoft.AspNetCore.Components;

namespace Mdazor;

internal static class ChildContentSplitter
{
    public record SplitResult(
        Dictionary<string, List<Block>> NamedFragments,
        List<Block> DefaultBlocks);

    public static SplitResult Split(Type componentType, ComponentBlock block)
    {
        var renderFragmentProperties = componentType.GetProperties()
            .Where(p => p.PropertyType == typeof(RenderFragment) ||
                        (p.PropertyType.IsGenericType &&
                         p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                         Nullable.GetUnderlyingType(p.PropertyType) == typeof(RenderFragment)))
            .ToList();

        var named = new Dictionary<string, List<Block>>();
        var defaults = new List<Block>();

        foreach (var child in block)
        {
            if (child is ComponentBlock childComponent)
            {
                var matching = renderFragmentProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, childComponent.ComponentName, StringComparison.OrdinalIgnoreCase));

                if (matching != null)
                {
                    if (!named.ContainsKey(matching.Name))
                        named[matching.Name] = new List<Block>();
                    named[matching.Name].AddRange(childComponent);
                    continue;
                }
            }

            defaults.Add(child);
        }

        return new SplitResult(named, defaults);
    }
}
