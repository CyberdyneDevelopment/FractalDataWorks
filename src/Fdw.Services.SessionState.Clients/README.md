# Fdw.Services.SessionState.Clients

The HTTP implementation of `ISessionStateService`, for UI applications.

The typed client for this domain's API, plus the payload models it sends and receives.

Client models carry the `Payload` suffix: the body shape, minus transport concerns. The server's endpoint layer names its own models `Request` / `Response`, and the domain type carries no suffix at all. The duplication is deliberate — it keeps the wire contract free to change without dragging the domain with it.

## Installation

```bash
dotnet add package Fdw.Services.SessionState.Clients --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.SessionState` · `Fdw.Services.SessionState.Abstractions` · `Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators` · `Fdw.Registration.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
