# API Clients Reference

Each FDW domain ships a `*.Clients` package that provides typed HTTP access to the matching `*.Endpoints` package. These clients are the HTTP layer in the [headless UI architecture](13-01-Headless-UI-Pattern.md), consumed by the headless logic providers in each `*.Components` package and available for any .NET consumer that needs programmatic access to FDW APIs.

## Architecture Overview

```
Consumer (UI / app)
  │
  └── Headless logic providers (*.Components packages)
        │
        └── Per-domain *.Clients packages (typed HTTP clients)
              │
              └── Fdw.Web.Clients (namespace Fdw.Web.Clients.Abstractions)
                    ApiClientBase   — HTTP plumbing (GET/POST/PUT/PATCH/DELETE) + RequestUri helper
                    ClientLog       — Structured logging (EventIds 11000-11002, 71000-71002, 91000-91003)
        └── Fdw.Web.Clients.Abstractions
                    Contracts/      — Cross-domain DTO interfaces
                    Caching/        — client-side result caching abstractions
```

Every typed API client:

1. **Inherits from `ApiClientBase`** — shared HTTP methods with JSON serialization, structured error handling, and logging.
2. **Returns `IGenericResult<T>`** — railway-oriented results that carry either a value or a structured error message. Never throws for business-logic failures.
3. **Registers via `ApiClientTypes`** — each client package contains a `[ServiceTypeOption(typeof(ApiClientTypes), "...")]` class inheriting `ApiClientTypeBase<TClient>` that is registered automatically by `Registration.SourceGenerators`.

## ApiClientBase

Located in `Fdw.Web.Clients/ApiClientBase.cs` (namespace `Fdw.Web.Clients.Abstractions`), the base class provides protected HTTP methods that wrap `HttpClient` calls with three catch blocks (`HttpRequestException`, `JsonException`, `Exception`) and emit structured logging via `ClientLog`. Every method returns `IGenericResult` or `IGenericResult<T>` — no exceptions cross the client boundary for business-logic failures. Its `RequestUri(path)` helper resolves the ABSOLUTE URI (base address + path) for every log call — a relative path alone hides which host/port the client actually hit, which is exactly the detail needed when a misconfigured base address is the fault.

See `public/src/Fdw.Web.Clients/ApiClientBase.cs` for the authoritative method signatures.

## ClientLog (EventIds 11000-11002, 71000-71002, 91000-91003)

`ClientLog` is a `[MessageLogging]` class in `Fdw.Web.Clients` (namespace `Fdw.Web.Clients.Abstractions`) that provides structured logging for all HTTP operations: sending request — Trace (11000), response received — Debug (11001), request completed — Information (11002), non-success status — Warning (71000), non-success status with response body — Warning (71001), HTTP transport error — Error (71002), deserialization failure — Error (91000), unexpected error — Error (91001), null response body — Error (91002), unrecognized list-response shape — Error (91003). Every message reports the ABSOLUTE request URI via `RequestUri`, and every exception-carrying message inlines the flattened `Exception.Message` as `{error}` so the single failure line names its cause. Every request is traced before sending, every response is logged with its status code, and every completion or failure produces a structured message that is both logged AND returned inside the `IGenericResult`.

## ApiClientTypes Registration

Client packages register through the `ApiClientTypes` TypeCollection:

```csharp
// In Services.Connections.Clients package:
[ServiceTypeOption(typeof(ApiClientTypes), "ConnectionClient")]
public sealed class ConnectionClientType : ApiClientTypeBase<ConnectionApiClient>
{
    // ...
}
```

Adding the client package's `PackageReference` to your app is the registration intent — the `Registration.SourceGenerators` module initializer wires the type into the collection at assembly load. `ApiClientTypes` is an ordinary `[ServiceTypeCollection]`, so it is driven by the same single `PlatformServices.Configure`/`Register`/`Initialize` sweep every other domain uses (see [12-01 Creating a Server](12-01-Creating-A-Server.md)) — there is no hand-written `ApiClientTypes.Configure(...)` call in `Program.cs`. Each registered type calls `services.AddHttpClient<TClient>(...)` with the configured base URL.

## Cross-Domain Contracts (`Web.Clients.Abstractions/Contracts/`)

The `Contracts/` directory defines interface abstractions for DTOs shared across domain boundaries (e.g. `IColumnSchema`, `IDataPreviewRequest`, `IDataPreviewResponse`, `IDataSetField`, `IEnvironmentInfo`). Per-domain `.Clients` packages implement these interfaces on their concrete DTOs, enabling dependency inversion between domain-specific clients.

**Rule:** a domain's `*.Clients.Abstractions` package must not duplicate DTOs that canonically belong to a lower-level package. If `Schema.Clients.Abstractions` needs a type owned by `Services.Data.Clients.Abstractions`, it adds a project reference and uses that type directly.

## Per-Package Inventory

The full client-package inventory is large (30+ packages) and changes over time. Rather than maintaining a duplicate list here, refer to the source:

- `public/src/FractalDataWorks.*.Clients/` for the typed client implementations
- `public/src/FractalDataWorks.*.Clients.Abstractions/` for the DTOs and interfaces
- Each `Clients` directory contains a `Registration/*ClientType.cs` declaring the `ApiClientTypes` registration

Every client follows the same shape:
- `XxxApiClient : ApiClientBase` — typed methods returning `IGenericResult<T>`
- `XxxClientType : ApiClientTypeBase<XxxApiClient>` — `[ServiceTypeOption(typeof(ApiClientTypes), "...")]`
- DTOs / requests / responses live in the matching `Abstractions` package

For the canonical wiring of clients into an app, see `reference-ui/public/Program.cs` and `reference-aui/public/src/Reference.Aui.Host/Program.cs`.

## See Also

- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) — provider/context/log triple consumed by clients
- [13-06 UI Skin Assembly Discovery](13-06-UI-Skin-Assembly-Discovery.md) — how skins discover client packages via `PageTypes`/`ApiClientTypes`
- [12-07 API Endpoints](12-07-API-Endpoints.md) — the server-side counterpart of these clients
