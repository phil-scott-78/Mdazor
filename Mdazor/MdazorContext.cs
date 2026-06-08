using Markdig;

namespace Mdazor;

/// <summary>
/// An ambient, read-only bag of host-supplied values made available to every Mdazor
/// component rendered from a single Markdown document (e.g. the current file name,
/// route, or front matter). A component reads it with
/// <c>[CascadingParameter] public MdazorContext? Context { get; set; }</c>.
/// </summary>
/// <remarks>
/// The context originates as a <see cref="MarkdownParserContext"/> passed to
/// <c>Markdown.Parse(markdown, pipeline, context)</c> (or <c>Markdown.ToHtml(...)</c>):
/// set it with <see cref="MdazorContextExtensions.SetMdazorContext"/>. Mdazor captures it
/// during parsing and surfaces it at render time as a <c>CascadingValue&lt;MdazorContext&gt;</c>.
/// Because cascading values do not cross a Blazor render-mode boundary, the context does
/// not automatically reach an interactive (WASM/Server) island; it is available on the
/// static string path and the prerendered/server render tree.
/// </remarks>
public sealed class MdazorContext
{
    /// <summary>The underlying value bag. Lookups honor whatever comparer the supplied dictionary uses.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>Creates a context over the supplied values.</summary>
    public MdazorContext(IReadOnlyDictionary<string, object?> values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>Gets the value for <paramref name="key"/>, or <c>null</c> when absent.</summary>
    public object? this[string key] => Values.TryGetValue(key, out var value) ? value : null;

    /// <summary>Attempts to get the value for <paramref name="key"/>.</summary>
    public bool TryGet(string key, out object? value) => Values.TryGetValue(key, out value);

    /// <summary>
    /// Gets the value for <paramref name="key"/> coerced to <typeparamref name="T"/>,
    /// or <c>default</c> when the key is missing or the value is not a <typeparamref name="T"/>.
    /// </summary>
    public T? Get<T>(string key) => Values.TryGetValue(key, out var value) && value is T typed ? typed : default;
}

/// <summary>
/// Bridges a <see cref="MarkdownParserContext"/> to Mdazor by carrying an <see cref="MdazorContext"/>
/// through Markdig's parse step so it can be surfaced to components at render time.
/// </summary>
public static class MdazorContextExtensions
{
    // Use the type itself as a collision-proof key in MarkdownParserContext.Properties.
    private static readonly object ContextKey = typeof(MdazorContext);

    /// <summary>
    /// Attaches host-supplied <paramref name="values"/> to the parser context so that components
    /// rendered from the parsed document can read them via a cascading <see cref="MdazorContext"/>.
    /// </summary>
    public static MarkdownParserContext SetMdazorContext(this MarkdownParserContext context, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(values);
        context.Properties[ContextKey] = new MdazorContext(values);
        return context;
    }

    /// <summary>Returns the <see cref="MdazorContext"/> previously attached via <see cref="SetMdazorContext"/>, or <c>null</c>.</summary>
    public static MdazorContext? GetMdazorContext(this MarkdownParserContext? context)
        => context is not null && context.Properties.TryGetValue(ContextKey, out var value)
            ? value as MdazorContext
            : null;
}
