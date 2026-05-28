using Mdazor;
using Mdazor.Demo.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The WASM client must register the same components as the server so that when
// Blazor hydrates a CSharpRepl boundary in the browser, the registry can resolve it.
builder.Services.AddMdazor()
    .AddMdazorComponent<CSharpRepl>(new InteractiveWebAssemblyRenderMode());

await builder.Build().RunAsync();
