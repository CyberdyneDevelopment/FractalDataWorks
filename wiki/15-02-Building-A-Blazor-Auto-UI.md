# Building a Blazor Auto UI

The canonical reference implementation of an FDW Blazor InteractiveAuto skin lives in the **reference-aui** repository.

`reference-aui/public/src/Reference.Aui.Host/Program.cs` is the authoritative shape for an InteractiveAuto skin using MudBlazor. The reference UI demonstrates:

- `AddInteractiveServerComponents()` + `AddInteractiveWebAssemblyComponents()` registration
- `new InteractiveAutoRenderMode(prerender: false)` on the routes
- `AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true)` so the auth state flows from server to WASM
- Dual-environment auth services: server-side providers (used during first visit) + WASM-side providers (used after transition)
- Discovery of FDW page packages via the `PageTypes` TypeCollection in `Routes.razor`

## Required Steps

To build a new InteractiveAuto skin:

1. **Create two projects** — a server project and a client (WASM) project. Both target net10.0.
2. **Reference the FDW UI packages** in both:
   - `Fdw.UI` (for `PageTypes`)
   - The per-domain `*.UI.Pages` packages you want surfaced
   - The per-domain `*.Components` and `*.Clients` packages (transitively pulled)
3. **Wire `Routes.razor`** to enumerate `PageTypes.All()` and pass the assemblies into `<Router AdditionalAssemblies="...">`.
4. **Register InteractiveAuto** in the server project's `Program.cs`:
   ```csharp
   builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents()
       .AddInteractiveWebAssemblyComponents()
       .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);
   ```
5. **Apply the render mode** in `App.razor`:
   ```razor
   <Routes @rendermode="new InteractiveAutoRenderMode(prerender: false)" />
   ```
6. **Register compatible auth services** in both `Program.cs` files (server-side using HttpClientFactory; WASM-side using browser HTTP and the deserialized auth state).

For the full startup of both halves, read the reference-aui Program.cs files directly.

## See Also

- [11-01 Management UI Overview](11-01-Management-UI-Overview.md)
- [11-02 Blazor Hosting Models](11-02-Blazor-Hosting-Models.md)
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md)
- [15-01 Building a Blazor Server UI](15-01-Building-A-Blazor-Server-UI.md) — InteractiveServer variant
