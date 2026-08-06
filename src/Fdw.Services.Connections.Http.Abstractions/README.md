# Fdw.Services.Connections.Http.Abstractions

Contracts for the HTTP connection and its protocol translators.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (5)

| Type | Kind | Purpose |
|---|---|---|
| `IHttpConnection` | interface | Marker interface for an HTTP connection. Exposes the base URL and the typed HTTP client primitive. |
| `IHttpMethod` | interface | Interface defining the contract for HTTP method enum options. |
| `IHttpProtocol` | interface | Interface defining the contract for HTTP protocol implementations. |
| `IHttpRecordWriterConnection` | interface | Optional connection capability: write a batch of records to an HTTP endpoint by serializing through the… |
| `IHttpSoapSettings` | interface | Interface for SOAP-specific settings. |

## Base types (7)

| Type | Kind | Purpose |
|---|---|---|
| `HttpMessageCollectionBase` | class | Collection definition to generate HttpMessages static class. |
| `HttpMethodBase` | class | Base class for HTTP method types in the TypeOption pattern. |
| `HttpMethodCollection` | class | Source generator creates static HttpMethods class automatically. |
| `HttpProtocolBase` | class | Base class for HTTP protocol types in the TypeOption pattern. |
| `HttpProtocols` | class | Collection of HTTP protocols for enhanced enum functionality. Source generator creates static… |
| `HttpResultCodeBase` | class | Base class for HTTP result codes. |
| `HttpResultCodes` | class | TypeCollection for HTTP connection result codes. EventId range: 5300-5399 (within Connections 5000-5999) |

## Models and supporting types (43)

| Type | Kind | Purpose |
|---|---|---|
| `CertificateLoadFailedCode` | class | Failed to load certificate. |
| `CommandTranslationFailedCode` | class | Failed to translate command to HTTP request. |
| `DeleteMethod` | class | HTTP DELETE method - deletes a resource from the server. |
| `GetMethod` | class | HTTP GET method - retrieves data from the server. |
| `GraphQLDeserializationFailedCode` | class | Failed to deserialize GraphQL data. |
| `GraphQLEmptyResponseCode` | class | Empty GraphQL response received. |
| `GraphQLErrorCode` | class | GraphQL server returned one or more errors. |
| `GraphQLHttpErrorCode` | class | GraphQL HTTP error response. |
| `GraphQLRequestBuildFailedCode` | class | Failed to build GraphQL request. |
| `GraphQLResponseParseFailedCode` | class | Failed to parse GraphQL response. |
| `HttpAuthenticationFailedMessage` | class | CurrentMessage indicating that HTTP authentication failed. |
| `HttpConfigurationInvalidMessage` | class | CurrentMessage indicating that HTTP configuration is invalid. |

## Installation

```bash
dotnet add package Fdw.Services.Connections.Http.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.RowSources.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Connections.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
