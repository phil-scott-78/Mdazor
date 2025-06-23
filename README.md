# Mdazor

A Markdig extension that lets you embed Blazor components directly in Markdown.

## What is this?

Ever wanted to write Markdown like this:

```markdown
# My Documentation

Here's some regular markdown content.

<AlertCard type="warning">
This is a **real Blazor component** with Markdown content inside!

- It supports lists
- And *formatting*
- And even nested components!

<Button variant="primary">Click me!</Button>
</AlertCard>

More regular markdown continues here...
```

And have it actually render real Blazor components? That's what this does.

## Why does this exist?

Writing documentation with static Markdown is fine, but sometimes you want to:

- Generate content programmatically
- Embed actual UI components in your docs

## How it works

This is a custom Markdig extension that:

1. **Parses component tags** - Recognizes `<ComponentName prop="value">content</ComponentName>` syntax in your Markdown
2. **Uses Blazor's HtmlRenderer** - Actually renders real Blazor components server-side
3. **Handles nested content** - Markdown inside components gets processed recursively
5. **Graceful fallback** - Unknown components just render as regular HTML tags

The magic happens in the parsing phase - we intercept component-looking tags before they get processed as regular HTML,
instantiate actual Blazor components, and render them using Blazor's server-side rendering.


## Example Components

Your components just need to be normal Blazor components:

```razor
@{
    var alertClass = Type switch
    {
        "success" => "alert-success",
        "warning" => "alert-warning",
        "danger" => "alert-danger",
        "info" => "alert-info",
        _ => "alert-primary"
    };
}

<div class="alert @alertClass" role="alert">
    @ChildContent
</div>

@code {
    [Parameter] public string Type { get; set; } = "primary";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

The `ChildContent` parameter gets populated with the processed Markdown content from inside the component tags.


## Features

### Component Syntax

```markdown
<!-- Self-closing components -->
<Icon name="star" size="24" />

<!-- Components with content -->
<Card title="Hello World">
This content becomes the component's ChildContent parameter.
</Card>

<!-- Nested components and markdown -->
<AlertBox type="info">
## This is a heading inside a component

And here's a nested component:
<Button onclick="alert('hi')">Click me</Button>

- Lists work too
- **Bold text**
- Everything you'd expect
</AlertBox>
```

### Fallback for unknown components

If a component isn't registered, it just renders as HTML:

```markdown
<SomeUnknownWidget foo="bar">content</SomeUnknownWidget>
```

Becomes: `<someunknownwidget foo="bar">content</someunknownwidget>` with the error message as an HTML comment.

## Setup

1. **Register your components**:

```csharp
services.AddMdazor()
    .AddMdazorComponent<AlertCard>()
    .AddMdazorComponent<Button>()
    .AddMdazorComponent<Icon>();
```

2. **Use it**:

Create your own pipeline, ensuring to inject your service provider so we can find the components:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UseMdazor(serviceProvider)
    .Build();
var html = Markdown.ToHtml(markdownContent, pipeline);
```

For advanced scenarios, you can still use the renderer directly:

```csharp
var document = Markdown.Parse(markdownContent, pipeline);
using var writer = new StringWriter();
var renderer = new BlazorRenderer(writer, componentRegistry, serviceProvider);
renderer.Render(document);
var html = writer.ToString();
```

## Technical Notes

- Uses Blazor's `HtmlRenderer` for actual server-side component rendering
- Component parameters are mapped using reflection with case-insensitive matching
- Nested Markdown content is processed recursively using the same pipeline
- Async rendering is fully supported
- `@inject` works inside components, so you can inject services as usual

## Limitations

- It doesn't handle whitespace all that great. If you are using a markdown element that relies on precise whitespace such as a code element or blockquote, don't indent your tags e.g.
    
    Do this:
    ``````
    <Card>
    ```csharp
    var i = 2 + 2;
    ```
    </Card>
    ``````

    Not this:
    ``````
    <Card>
        ```csharp
        var i = 2 + 2;
        ```
    </Card>
    ``````
- Server-side rendering only (no client-side Blazor support yet)
- Components need to be registered ahead of time
- No support for complex parameter types (just strings, numbers, bools for now)
- Component names must start with a capital letter (follows HTML custom element rules)

## Project Structure

- `Mdazor/` - The main library with the Markdig extension
- `Mdazor.Demo/` - A Blazor Server app showing it in action
- `Mdazor.Tests/` - Unit and integration tests

## Running the Demo

```bash
cd Mdazor.Demo
dotnet run
```

Then check out the demo pages to see components embedded in Markdown.

## Contributing

This is a skunkworks project, so don't expect enterprise-level polish. But if you find bugs or have ideas, PRs welcome!

The main areas that could use work:

- Support for more complex parameter types
- Fixing the aforementioned whitespace problems
- Client-side Blazor support
- Better error handling and diagnostics
- Performance optimizations
- More comprehensive documentation

## License

MIT - do whatever you want with it.