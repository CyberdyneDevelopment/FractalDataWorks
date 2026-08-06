# Fdw.VsCodeShell

The VS Code shell host.

## Types (5)

| Type | Kind | Purpose |
|---|---|---|
| `VsCodeShellContributesExport` | class | Emits the contributes block for the staged package.json from the registered command collection, so… |
| `VsCodeShellLog` | class | Message logging for the VS Code shell's command dispatch surface. EventId ranges follow the result-code… |
| `VsCodeShellOptions` | class | Required identity fields a host extension supplies via AddVsCodeShell(...). These appear in the manifest… |
| `VsCodeShellApplicationBuilderExtensions` | class | — |
| `VsCodeShellServiceCollectionExtensions` | class | — |

## Installation

```bash
dotnet add package Fdw.VsCodeShell --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.VsCodeShell.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
