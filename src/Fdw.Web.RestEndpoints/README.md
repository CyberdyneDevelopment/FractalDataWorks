# Fdw.Web.RestEndpoints

REST endpoint infrastructure and the error shape a failure is rendered as.

This package declares 1 interface(s), 1 service/provider type(s), 12 configuration type(s), 8 model(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IETagProvider` | interface | Provides ETag generation for conditional GET support on CRUD endpoints. Implementations determine the… |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `RowIdETagProvider` | class | ETag provider that computes ETags from the most recent RowId in a container. Uses the DataGateway to… |

## Configuration (12)

| Type | Kind | Purpose |
|---|---|---|
| `ApiKeyConfiguration` | class | API key authentication configuration. |
| `ApiKeySecurityConfiguration` | class | API key-specific security configuration. |
| `AuthenticationConfiguration` | class | Authentication configuration for the web framework. |
| `CertificateSecurityConfiguration` | class | Certificate-based security configuration. |
| `CorsConfiguration` | class | CORS configuration for cross-origin requests. |
| `CorsSecurityConfiguration` | class | CORS security configuration. |
| `JwtConfiguration` | class | JWT authentication configuration. |
| `JwtSecurityConfiguration` | class | JWT-specific security configuration. |
| `OAuth2SecurityConfiguration` | class | OAuth2-specific security configuration. |
| `SecurityConfiguration` | class | Security configuration for the Fdw Web Framework. Provides centralized security settings and validation. |
| `SwaggerConfiguration` | class | Swagger/OpenAPI documentation configuration. |
| `WebConfiguration` | class | Main web configuration implementation for the Fdw Web Framework. Provides concrete configuration with… |

## Records (8)

| Type | Kind | Purpose |
|---|---|---|
| `ByPropertyRequest<TKey>` | class | Uniform request DTO for endpoints that look up a resource by a single key property (Name, Id, Code,… |
| `ErrorResponse` | class | Structured error response returned to API clients. Contains only user-safe information — no server… |
| `PagedRequest` | class | Base class for paginated requests. Provides standard pagination parameters with validation. |
| `PagedResponse<T>` | class | Generic paginated response wrapper. Contains data and pagination metadata. |
| `PaginatedListRequest` | class | Request model for paginated list endpoints. Provides skip/take parameters for offset-based pagination. |
| `PaginatedResponse<T>` | class | Response wrapper for paginated list endpoints. Contains the items for the current page and pagination… |
| `StreamingRequest` | class | Base class for streaming requests that process large datasets. Provides parameters for controlling… |
| `UpdateByPropertyRequest<TKey, TBody>` | class | Uniform request DTO for endpoints that update a resource located by a single key property. The key is… |

## Types (39)

| Type | Kind | Purpose |
|---|---|---|
| `AdminEndpointBase<TResponse>` | class | Abstract base class for administrative endpoints that require the admin authorization policy. Rate… |
| `AdminEndpointBase<TRequest, TResponse>` | class | Abstract base class for administrative endpoints with a request body. Rate limiting defaults to (10000… |
| `ApiKeyMetadata` | class | Metadata associated with an API key. |
| `ApplicationBuilderExtensions` | class | Extension methods for configuring the Fdw Web Framework middleware pipeline. Provides fluent API for… |
| `AuthenticatedEndpointBase<TResponse>` | class | Abstract base class for authenticated endpoints that require valid credentials. Rate limiting defaults… |
| `AuthenticatedEndpointBase<TRequest, TResponse>` | class | Abstract base class for authenticated endpoints with a request body. Rate limiting defaults to (500… |
| `CommandEndpoint<TCommand>` | class | Command endpoint for commands that don't return specific data (void commands). Returns a success/failure… |
| `CommandEndpoint<TCommand, TResult>` | class | Base class for CQRS command operations with RBAC and validation. Commands are operations that modify… |
| `CrudCreateEndpoint<TCreateRequest, TDetail>` | class | Abstract base class for resource creation endpoints. Provides uniqueness checking (409), validation,… |
| `CrudDeleteEndpoint<TRequest>` | class | Abstract base class for resource deletion endpoints. Provides existence checking (404), pre-delete… |
| `CrudGetEndpoint<TRequest, TDetail>` | class | Abstract base class for get-by-name endpoints. Provides routing, authorization, 404 handling, error… |
| `CrudListEndpoint<TSummary>` | class | Abstract base class for list/summary endpoints that return a collection of resources. Provides… |

## Installation

```bash
dotnet add package Fdw.Web.RestEndpoints --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Operations.Abstractions` · `Fdw.Orchestration.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Scheduling.Abstractions` · `Fdw.Services.Settings` · `Fdw.Web.Endpoints` · `Fdw.Web.Http.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
