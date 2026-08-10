# Fdw.UI.Lineage.Clients.Abstractions

The payload models for the lineage graph UI.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Installation

```bash
dotnet add package Fdw.UI.Lineage.Clients.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.UI.Pipelines.Clients.Models`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
