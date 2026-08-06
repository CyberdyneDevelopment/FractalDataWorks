# Fdw.VsCodeShell.Abstractions

VS Code shell contracts and its command collection.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `VsCodeCommandTypes` | class | ServiceTypeCollection of every VS Code command contributed by the host and its referenced packages. The… |

## Options (0 declared)

| Type | Kind | Purpose |
|---|---|---|
| `IVsCodeCommandType` | interface | Non-generic marker for a VS Code command declared as a [ServiceTypeOption]. The collection is keyed on… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Installation

```bash
dotnet add package Fdw.VsCodeShell.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Results.Abstractions` · `Fdw.Services.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
