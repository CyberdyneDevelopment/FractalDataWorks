# UI Skin Assembly Discovery

How Blazor's router discovers pages from FDW `*.UI.Pages` packages — the `PageTypes` TypeCollection, the `Routes.razor` scanning pattern, and the skin contract for alternative implementations.

---

## The Problem

Blazor's `<Router>` only scans the entry assembly (`typeof(Program).Assembly`) for `@page` components. FDW packages pages in separate `*.UI.Pages` assemblies, so without explicit configuration those routes are invisible and navigating to them returns 404.

---

## The Solution: `PageTypes` TypeCollection

Each FDW `*.UI.Pages` package contributes a `[TypeOption]` of `PageTypes` that exposes the assembly to the router and declares the package's navigation entries.

```csharp
// Fdw.Services.Connections.UI.Pages/ConnectionsPageType.cs
[TypeOption(typeof(PageTypes), "Connections")]
public sealed class ConnectionsPageType : PageTypeBase
{
    public ConnectionsPageType()
        : base(10, "Connections", typeof(ConnectionsPageType).Assembly) { }

    public override IReadOnlyList<NavDescriptor> NavItems { get; } =
    [
        new NavDescriptor
        {
            Section = "Data Sources",
            SectionKey = "data-sources",
            SectionOrder = 10,
            Label = "Connections",
            Route = "/connections",
            Icon = "link",
            Order = 10
        }
    ];
}
```

`PageTypeBase` carries the page assembly and nav metadata:

```csharp
public abstract class PageTypeBase : TypeOptionBase<int, PageTypeBase>, IPageType
{
    public Assembly PageAssembly { get; }
    public abstract IReadOnlyList<NavDescriptor> NavItems { get; }
    // ...
}
```

`Routes.razor` in the skin project enumerates every registered `PageTypes` option, extracts the `PageAssembly`, and hands the array to `<Router AdditionalAssemblies="...">`:

```razor
@using System.Reflection
@using Fdw.UI.Navigation

@code {
    private static readonly Assembly[] _fdwPageAssemblies =
        PageTypes.All()
            .Select(p => p.PageAssembly)
            .ToArray();
}

<Router AppAssembly="typeof(Program).Assembly" AdditionalAssemblies="_fdwPageAssemblies">
    ...
</Router>
```

Adding a new `*.UI.Pages` package to the skin's `.csproj` is the only step required — the module initializer emitted by `Registration.SourceGenerators` registers the `[TypeOption]` at assembly load, `Routes.razor` discovers it automatically, and the package's `NavItems` flow into the nav rendering.

---

## Packages That Carry a `PageType`

Each FDW page package declares one `[TypeOption(typeof(PageTypes), "...")]` class. Per-package routes and nav entries are owned by that `PageType` class — refer to the package source for the current set.

---

## Skin Contract: What a Skin References

A skin project (e.g., reference-ui, a customer skin) assembles its page set by choosing which packages to reference:

| Reference | What you get |
|-----------|-------------|
| `*.Clients` | Named HttpClient auto-registered via `ApiClientTypes` |
| `*.Components` | Headless provider + context (state, callbacks, no HTML) |
| `*.UI.Pages` | FDW default page implementations + `PageType` registration (opt-in) |

**Using the FDW default pages** — add the `*.UI.Pages` `PackageReference`. The package's `[TypeOption]` is registered on assembly load and picked up by `PageTypes.All()` in `Routes.razor`.

**Replacing a domain's pages** — omit the `*.UI.Pages` reference and write your own pages. Your pages reference `*.Components` directly for the provider and context.

**Mixing** — reference FDW pages for some domains and write custom pages for others. The two sets coexist; FDW pages are discovered via `PageTypes.All()`, custom pages via `typeof(Program).Assembly` in the main skin project.

---

## Why Not Name-Based Discovery?

An alternative is to scan referenced assemblies by the `*.UI.Pages` naming convention:

```csharp
.Where(n => n.Name?.EndsWith(".UI.Pages") == true)
```

This is fragile — a package rename breaks discovery silently. The `PageTypes` TypeCollection approach is **explicit opt-in**: the package author writes the `[TypeOption]` class, controls the package's identity (`Name`/`Id`), and ships the nav metadata in the same place. A third-party package that happens to end in `.UI.Pages` is not mistakenly included.

---

## Why Not `[RouteAttribute]` Scanning?

`@page "/route"` compiles to `[RouteAttribute]` on the component class, so you could discover page assemblies by scanning for types with `RouteAttribute`. But this calls `GetTypes()` on every referenced assembly — slow, and throws `ReflectionTypeLoadException` on assemblies with missing dependencies. The TypeCollection lookup is O(1) per registered option and never throws.

---

## See Also

- [13-05 UI Layer Anatomy](13-05-UI-Layer-Anatomy.md) — per-layer object types and rules for `*.UI.Pages`
- [16-03 UI Domain Map](16-03-UI-Domain-Map.md) — per-domain package and page inventory
- [15-01 Building a Blazor Server UI](15-01-Building-A-Blazor-Server-UI.md) — complete skin setup tutorial
