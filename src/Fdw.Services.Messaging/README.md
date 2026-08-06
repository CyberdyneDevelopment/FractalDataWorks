# Fdw.Services.Messaging

In-application messaging: messages, recipients and access requests.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `MessagingServiceTypes` | class | ServiceTypeCollection for messaging service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultMessagingServiceType` | class | Default messaging service type that registers and with the dependency injection container. |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Installation

```bash
dotnet add package Fdw.Services.Messaging --prerelease
```

## Dependencies

`Fdw.Commands.Data.Extensions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Messaging.Abstractions` · `Fdw.SignalR`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
