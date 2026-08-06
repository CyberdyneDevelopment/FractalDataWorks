# Building a Blazor Server UI

The canonical reference implementation of an FDW Blazor InteractiveServer skin lives in the **reference-ui** repository.

`reference-ui/public/Program.cs`, `reference-ui/public/Components/Routes.razor`, and `reference-ui/public/Components/App.razor` are the authoritative shape for an InteractiveServer skin using Tailwind CSS v4. The reference UI demonstrates:

- `AddInteractiveServerComponents()` registration
- `@rendermode="RenderMode.InteractiveServer"` on the routes
- Tailwind CSS build via the project-local `tailwindcss.exe` MSBuild target
- JWT auth with sessionStorage (Blazored.SessionStorage)
- `DelegatingHandler`-based auth handler attaching JWTs to outgoing API calls
- Discovery of FDW page packages via the `PageTypes` TypeCollection in `Routes.razor`

## Required Steps

To build a new InteractiveServer skin:

1. **Create a Blazor Web project** (Microsoft.NET.Sdk.Web, target net10.0) with InteractiveServer enabled.
2. **Reference the FDW UI packages:**
   - `Fdw.UI` (for `PageTypes`)
   - The per-domain `*.UI.Pages` packages you want surfaced (each registers its own `[ServiceTypeOption(typeof(PageTypes), "...")]`)
   - The per-domain `*.Components` packages (transitively pulled by `*.UI.Pages`)
   - The per-domain `*.Clients` packages (transitively pulled by `*.Components`)
3. **Wire `Routes.razor`** to enumerate `PageTypes.All()` and pass `.Select(p => p.PageAssembly).ToArray()` into `<Router AdditionalAssemblies="...">`.
4. **Wire up auth, API clients, and headless logic providers** in `Program.cs` — see the reference-ui Program.cs for the canonical wiring.
5. **Add the Tailwind build target** (or your preferred CSS pipeline). reference-ui ships a `tailwindcss.exe` MSBuild `BeforeTargets="Build"` target.

## See Also

- [11-01 Management UI Overview](11-01-Management-UI-Overview.md)
- [11-02 Blazor Hosting Models](11-02-Blazor-Hosting-Models.md)
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md)
- [13-06 UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md)
- [15-02 Building a Blazor Auto UI](15-02-Building-A-Blazor-Auto-UI.md) — InteractiveAuto variant
