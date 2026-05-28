# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & test

```bash
dotnet build Mdazor.sln
dotnet test Mdazor.Tests/Mdazor.Tests.csproj
dotnet test Mdazor.Tests/Mdazor.Tests.csproj --filter "FullyQualifiedName~IntegrationTests"
dotnet test Mdazor.Tests/Mdazor.Tests.csproj --filter "FullyQualifiedName=Mdazor.Tests.IntegrationTests.RenderSimpleComponent_ShouldProduceCorrectHtml"
cd Mdazor.Demo && dotnet run     # runs the demo Blazor Server app
```

`global.json` pins SDK `10.0.100` with `rollForward: latestFeature`, but the projects (`Mdazor`, `Mdazor.Tests`, `Mdazor.Demo`) target **`net9.0`**. Use xUnit (v2) + Shouldly for assertions.

CI (`.github/workflows/ci.yml`) runs `dotnet-releaser` on pushes to `main`/`master`, which builds, tests, and publishes a NuGet package. Versioning is driven by **MinVer** (git tags) — no manual version bump in the csproj.

## Architecture

Mdazor is a **Markdig extension** that intercepts custom-element-style tags in Markdown (`<Foo bar="...">...</Foo>`) and renders them as real Razor components using Blazor's server-side `HtmlRenderer`.

The pipeline has four layers; understanding their interplay is the key to working in this codebase:

1. **Parsing** — Two parsers fork on `<` as the opening character:
   - `ComponentBlockParser` (block) handles tags that occupy whole lines, including multi-line content with a matching `</Foo>` closer. It pushes a `ComponentBlock` (a `ContainerBlock`, so child Markdown blocks parse into it normally). Critically, `TryContinue` calls `processor.GoToColumn(processor.ColumnBeforeIndent)` so nested code fences/blockquotes keep their original column — without this, indentation-sensitive Markdown breaks inside a component.
   - `ComponentInlineParser` (inline) only matches **self-closing** tags (`<Icon name="x" />`) embedded in a paragraph.
   - Both require a **capital-letter prefix** on the tag name (HTML custom-element rule); lowercase tags fall through to Markdig's normal HTML handling.

2. **Component registry** (`IComponentRegistry` / `ComponentRegistry`) — name → `Type` map populated at DI-time. Lookups by name are how the renderer decides "real component" vs. "unknown — emit as plain HTML."

3. **DI wiring** (`ServiceCollectionExtensions`) — `AddMdazor()` + `AddMdazorComponent<T>()` build up a `ComponentRegistrationsHolder` (singleton, mutated during registration) which is replayed when the `IComponentRegistry` is first resolved. Two non-obvious choices:
   - `HtmlRenderer` is registered as **transient**, not scoped/singleton. If you change this, DI throws `Cannot resolve scoped service 'ICascadingValueSupplier' from the root provider`.
   - Pipeline is built via `new MarkdownPipelineBuilder().UseMdazor(serviceProvider).Build()` — `UseMdazor` reaches into the service provider for `IComponentRegistry` and constructs `MdazorExtension`.

4. **Rendering** (`ComponentBlockRenderer`, `ComponentInlineRenderer`) — These are added to Markdig's existing `HtmlRenderer` via `MdazorExtension.Setup`. For each component:
   - Attributes → component parameters via reflection with case-insensitive property matching and primitive coercion (`string`/`int`/`bool`/`double`; everything else stays a string).
   - Child blocks → `ChildContent` `RenderFragment` by recursively rendering the inner Markdown to HTML and wrapping with `builder.AddMarkupContent`.
   - **Named RenderFragments**: if a child element's name matches a `RenderFragment`-typed property on the component (e.g. `<Header>...</Header>` inside `<Card>`), its inner content goes into that named slot instead of `ChildContent`. This is how multi-slot components work.
   - Final render goes through `htmlRenderer.Dispatcher.InvokeAsync(...)` and is bridged to Markdig's sync world by `AsyncHelper.RunSync` (custom `TaskFactory` to avoid sync-context deadlocks).
   - Failures fall back to writing the tag as raw HTML plus an HTML comment with the exception message.

### Three renderer code paths

There are **three** paths. Two produce static HTML strings; the third produces a live Blazor render tree (the only one that can carry interactivity).

1. **`Markdown.ToHtml(...)` (string path)** — `MdazorExtension` injects `ComponentBlockRenderer`/`ComponentInlineRenderer` into Markdig's standard `HtmlRenderer`. Components are rendered to an HTML string via the server-side `HtmlRenderer`; the result is inert markup — no component boundary survives, so render modes are meaningless here.
2. **`BlazorRenderer`** — an alternative string-path entry point (README "advanced scenarios") that subclasses Markdig's `HtmlRenderer` with private nested versions of the same renderers.
3. **`<MdazorContent Markdown="..." />` + `MarkdownRenderTreeWriter` (render-tree path)** — the interactive path. Instead of emitting an HTML string, it walks the parsed `MarkdownDocument` and writes directly into a `RenderTreeBuilder`: known components become real `OpenComponent` frames (so `ChildContentSplitter` slotting, `key=` → `SetKey`, and per-component **render modes** via `GetRenderMode` → `AddComponentRenderMode` all apply), while non-component blocks fall back to `AddMarkupContent` of their static HTML. Inline components inside a paragraph are handled by `PlaceholderInlineRenderer`, which wraps each in a U+001E-delimited index token so `MarkdownRenderTreeWriter` can split the paragraph HTML and splice the real component frames back in. **This is what lets a `<Foo />` tag in Markdown become a prerendered-then-hydrated WASM/Server island** — register the component with a render mode (`AddMdazorComponent<T>(new InteractiveWebAssemblyRenderMode())`) and host it under a Blazor Web App with the matching `.AddInteractive*RenderMode()`. See `Mdazor.Demo` + `Mdazor.Demo.Client` (`CSharpRepl`) for the end-to-end wiring.

When fixing a rendering bug, **check all three** — the attribute coercion (`AttributeConverter`) and named-fragment/child-content split (`ChildContentSplitter`) are now shared, but the surrounding logic in `BlazorRenderer.RenderComponentAsync`, `ComponentBlockRenderer.RenderComponentAsync`, and `MarkdownRenderTreeWriter` is still separate.

## Conventions worth knowing

- Attribute regex requires **quoted values** (`name="x"` or `name='x'`); bare or unquoted attributes are ignored.
- Component-tag detection is regex-based on a single line (block) or slice (inline). Multi-line opening tags (attributes wrapped across lines) are not supported.
- Indenting the inside of a block component breaks fenced code/blockquotes — see README "Limitations". Tests in `WhitespaceTests.cs` pin this behavior.
- All service-registration extension methods return `IServiceCollection` for chaining (recently standardized — keep new ones consistent).
