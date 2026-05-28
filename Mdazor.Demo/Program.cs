using Mdazor;
using Mdazor.Demo.Client;
using Mdazor.Demo.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMdazor()
    .AddMdazorComponent<Alert>()
    .AddMdazorComponent<Card>()
    .AddMdazorComponent<Button>()
    // CSharpRepl lives in the client assembly and runs in WASM. The server registers
    // it with the same render mode so SSR emits the correct interactivity boundary.
    .AddMdazorComponent<CSharpRepl>(new InteractiveWebAssemblyRenderMode());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Mdazor.Demo.Client._Imports).Assembly);

app.Run();
