# Fdw.Services.Connections.Http

The HTTP connection: protocol-agnostic, reaching REST, OData, GraphQL or SOAP through an injected translator rather than a per-protocol connection.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `HttpAuthenticationTypes` | class | TypeCollection for HTTP security configurations. Each entry is both a TypeOption (identity + factory)… |
| `HttpConnectionLimitTypes` | class | TypeCollection of outbound connection limit options for Http connections. Mirrors… |
| `SoapSecurityProcessors` | class | TypeCollection for SOAP security processors. |

## Options (32 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ApiKeySecurityConfiguration` | class | API key authentication configuration. The API key is passed in a request header (default: X-API-Key). |
| `ApolloFederationProtocol` | class | Apollo Federation compatible GraphQL protocol. |
| `BasicSecurityConfiguration` | class | HTTP Basic authentication configuration. |
| `BearerSecurityConfiguration` | class | Bearer token authentication configuration. Placeholder for future token-based authentication support. |
| `GraphQLProtocol` | class | Standard GraphQL protocol implementation. |
| `GraphQLSubscriptionsProtocol` | class | GraphQL protocol with subscription support via WebSocket. |
| `JsonApiProtocol` | class | JSON:API specification protocol implementation. |
| `MaxConcurrentRequestsType` | class | TypeOption for the MaxConcurrentRequests connection limit kind on Http connections. Controls max… |
| `MaxRequestRateType` | class | TypeOption for the MaxRequestRate connection limit kind on Http connections. Controls outbound request… |
| `MaxResponseSizeType` | class | TypeOption for the MaxResponseSize connection limit kind on Http connections. Caps the response payload… |
| `NoneSecurityConfiguration` | class | No security applied — pass-through configuration. |
| `NoneSoapSecurityProcessor` | class | No-op security processor that passes the envelope through unchanged. |
| `ODataProtocol` | class | OData-style REST protocol implementation. |
| `RequestTimeoutType` | class | TypeOption for the RequestTimeout connection limit kind on Http connections. Cancels outbound HTTP… |
| `RestProtocol` | class | Standard REST protocol implementation using common conventions. |
| `Soap11Protocol` | class | SOAP 1.1 protocol implementation. |
| `Soap12Protocol` | class | SOAP 1.2 protocol implementation. |
| `SourceMissingHttpEndpointCode` | class | Source configuration is missing HttpEndpoint — cannot resolve to a container. |

## Installation

```bash
dotnet add package Fdw.Services.Connections.Http --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Data.DataContainers.Abstractions` · `Fdw.Data.DataNodes` · `Fdw.Data.DataSets` · `Fdw.Data.Files` · `Fdw.Data.Http` · `Fdw.Data.RowSources` · `Fdw.Data.RowSources.Abstractions` · `Fdw.Data.RowSources.FixedWidth.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Http.Abstractions` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
