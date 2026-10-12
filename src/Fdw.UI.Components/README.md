# Fdw.UI.Components

The headless component library — state and behaviour without a fixed skin.

Framework-neutral UI models the renderers draw from: page models (`Pages`), form field models
(`Models`), icon glyphs (`Icons`), status badge and colour variants (`Services`), and the
TypeCollection select model (`TypeIntegration`). The Blazor components and their base classes were
removed (FDW-872); this package carries no Razor or ASP.NET dependency.

## Installation

```bash
dotnet add package Fdw.UI.Components --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.UI.Abstractions` · `Fdw.UI.Web.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
