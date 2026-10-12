# Fdw.Intelligence

Intelligence services.

## Types (3)

| Type | Kind | Purpose |
|---|---|---|
| `IntelligenceLog` | class | MessageLogging for Intelligence Service operations. EventId range: 7060-7079 |
| `MemorySidecar` | class | Background worker that monitors agent state and provides semantic recall triggers. Implements the "Deja… |
| `VectorMemoryStore` | class | In-memory implementation of the vector memory store. Uses keyword matching for recall (prototype — will… |

## Installation

```bash
dotnet add package Fdw.Intelligence --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Intelligence.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
