# 12-03 Service Communication


This guide documents the inter-service communication patterns used across the FractalDataWorks reference solution. The architecture follows an API gateway pattern where reference-api acts as the single entry point for all external clients, proxying requests to backend services as needed.

---

## Architecture Diagram

```
                                 +------------------+
                                 |   reference-ui   |
                                 |   (port 5007)    |
                                 +--------+---------+
                                          |
                                    HTTPS (JWT)
                                          |
                                          v
                              +-----------+-----------+
                              |      reference-api      |
                              |      (port 5001)      |
                              |                       |
                              |  FastEndpoints + JWT  |
                              |  SignalR Hubs         |
                              |  Proxy Endpoints      |
                              +-+--------+----------+-+
                                |        |          ^
                          HTTP  |        | HTTP     | HTTP (webhook)
                                |        |          |
                   +------------+    +---+------+   |
                   |                 |          |   |
                   v                 v          +---+
         +---------+------+  +------+--------+
         | reference-scheduler|  |   reference-etl   |
         |  (port 5004)   |  |  (port 5002)  |
         +--------+-------+  +---------------+
                  |                   ^
             HTTP | (dispatch)        |
                  +-------------------+
```

**Communication flows:**

1. **reference-ui --> reference-api** -- All UI requests go through the API gateway over HTTPS with JWT bearer tokens.
2. **reference-api --> reference-scheduler** -- Proxy endpoints use `IScheduleClient` (typed client) to forward schedule management requests.
3. **reference-api --> reference-etl** -- Proxy endpoints use `IPipelineJobClient` (typed client) to forward ETL trigger requests.
4. **reference-scheduler --> reference-etl** -- The `EtlDispatchService` uses `IPipelineJobClient` to dispatch ETL jobs when schedules fire, with exponential backoff retry.
5. **reference-etl --> reference-api** -- Webhook callbacks notify reference-api when ETL pipeline executions complete.

All three backend servers use FastEndpoints with a consistent `api/v1` route prefix.

---

## API Gateway Pattern

reference-ui never communicates directly with reference-scheduler or reference-etl. All requests are routed through reference-api, which acts as the API gateway. This design provides a single authentication boundary, a single CORS origin, and a single TLS termination point.

### Service Endpoint Configuration

reference-api discovers backend service URLs through the `ServiceEndpoints` configuration section:

```json
{
  "ServiceEndpoints": {
    "Scheduler": "http://localhost:5004",
    "Etl": "http://localhost:5002"
  }
}
```

This is bound to the `ServiceEndpointsOptions` class in `Reference.Api/Configuration/ServiceEndpointsOptions.cs`:

```csharp
public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";
    public string Scheduler { get; set; } = string.Empty;
    public string Etl { get; set; } = string.Empty;
}
```

### Typed API Client Architecture

Inter-service communication uses **typed API client packages** instead of raw `HttpClient`. Each backend service has a pair of framework packages:

| Abstractions Package | Implementation Package | Interface | HTTP Client | Purpose |
|---------------------|----------------------|-----------|-------------|---------|
| `Fdw.Services.Pipelines.Clients.Abstractions` | `Fdw.Services.Pipelines.Clients` | `IPipelineClient` | `PipelineHttpClient` | Pipeline configuration (list, get) |
| `Fdw.Services.Pipelines.Clients.Abstractions` | `Fdw.Services.Pipelines.Clients` | `IPipelineJobClient` | `PipelineJobHttpClient` | ETL job execution (trigger, status) |
| `Fdw.Services.Scheduling.Clients.Abstractions` | `Fdw.Services.Scheduling.Clients` | `IScheduleClient` | `ScheduleHttpClient` | Schedule management |

This architecture follows the standard FDW dependency inversion pattern: proxy endpoints and services depend on the interface (from `.Abstractions`), and the DI container resolves the HTTP implementation. The HTTP implementations inherit from `ApiClientBase` (in `Fdw.Web.Clients.Abstractions`) which provides `Get<T>()`, `Post<TReq,TRes>()`, `Put<T>()`, and `Delete()` methods with structured error handling via `IGenericResult<T>`.

### Client Registration

Typed clients are registered in `ProxyServiceExtensions.AddProxyServices()`:

```csharp
services.AddHttpClient<IPipelineJobClient, PipelineJobHttpClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ServiceEndpointsOptions>>();
    client.BaseAddress = new Uri(options.Value.Etl + "/api/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<IScheduleClient, ScheduleHttpClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ServiceEndpointsOptions>>();
    client.BaseAddress = new Uri(options.Value.Scheduler + "/api/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Note:** `IPipelineClient` (pipeline configuration list/get) targets reference-api's own endpoints at `/api/v1/pipelines` and is not proxied. Consumers register it with the reference-api base URL.

### Proxy Endpoints

All proxy endpoints are FastEndpoints classes that inject the typed client interfaces. The reference-api uses a route prefix of `api/v1`, and the proxy endpoints add a `proxy/` segment:

| Endpoint Class | Method | Route | Typed Client | Target Path |
|---------------|--------|-------|--------------|-------------|
| `GetSchedulesProxyEndpoint` | GET | `/api/v1/proxy/schedules` | `IScheduleClient` | `GET /api/v1/schedules` |
| `CreateScheduleProxyEndpoint` | POST | `/api/v1/proxy/schedules` | `IScheduleClient` | `POST /api/v1/schedules` |
| `TriggerEtlJobProxyEndpoint` | POST | `/api/v1/proxy/etl/trigger` | `IPipelineJobClient` | `POST /api/v1/etl/trigger` |
| `EtlWebhookEndpoint` | POST | `/api/v1/proxy/etl/webhook/completion` | (receives inbound) | N/A |

**Note:** Pipeline configuration endpoints (`GET /api/v1/pipelines`, `GET /api/v1/pipelines/{name}`) are served directly by reference-api (not proxied) since pipeline configuration is stored in ConfigurationDb.

**Error handling:** When a typed client call fails, the endpoint returns HTTP 502 (Bad Gateway) with a JSON error body containing the upstream error details. All proxy operations are logged through `ProxyLog` (EventId range 1900-1905).

### Proxy Request Flow Example

A typical schedule creation request flows as follows:

```
reference-ui                reference-api                     reference-scheduler
    |                           |                                |
    |  POST /api/v1/proxy/      |                                |
    |       schedules           |                                |
    |  { Name, PipelineName,    |                                |
    |    CronExpression, ... }  |                                |
    |-------------------------->|                                |
    |                           |  IScheduleClient               |
    |                           |    .CreateSchedule(req)        |
    |                           |  → POST /api/v1/schedules      |
    |                           |-------------------------------->|
    |                           |                                |
    |                           |  201 Created (JSON)            |
    |                           |<-------------------------------|
    |  201 Created (JSON)       |                                |
    |<--------------------------|                                |
```

The proxy endpoint calls `IScheduleClient.CreateSchedule()` which is resolved by DI to `ScheduleHttpClient`, which makes the actual HTTP POST to the reference-scheduler.

---

## Internal API Key Authentication

For service-to-service communication that does not carry a user JWT (such as webhook callbacks and scheduler-to-ETL dispatch), FractalDataWorks provides `InternalApiKeyMiddleware` in the `Fdw.Hosting` package.

### How It Works

The middleware checks every incoming request for a header containing a shared secret:

1. Read the `InternalApi` configuration section to get the expected key and header name.
2. If `ApiKey` is empty, **bypass authentication entirely** (development mode).
3. If the request does not contain the expected header, or the value does not match, return **401 Unauthorized**.
4. If the key matches, pass the request through to the next middleware.

### Configuration

```json
{
  "InternalApi": {
    "ApiKey": "your-secret-api-key-here",
    "HeaderName": "X-Internal-Api-Key"
  }
}
```

The `InternalApiKeyOptions` class in `Fdw.Hosting/Configuration/InternalApiKeyOptions.cs`:

```csharp
public sealed class InternalApiKeyOptions
{
    public const string SectionName = "InternalApi";
    public string ApiKey { get; set; } = string.Empty;
    public string HeaderName { get; set; } = "X-Internal-Api-Key";
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `ApiKey` | `""` (disabled) | The shared secret. Empty string disables validation entirely. |
| `HeaderName` | `X-Internal-Api-Key` | HTTP header name to check for the key. |

### Security Considerations

- **Development mode:** When `ApiKey` is empty, the middleware passes all requests through without checking. This allows local development without configuring keys, but must never be deployed to production with an empty key.
- **Header comparison:** The key comparison uses `StringComparison.Ordinal` for exact, case-sensitive matching.
- **Production deployment:** Use a cryptographically random key of at least 32 bytes. Store the key in a secret manager rather than in appsettings.json. All services in the topology must share the same key value.

---

## Webhook Callbacks

When reference-etl finishes executing a pipeline, it posts a completion notification back to reference-api via HTTP webhook. This inverted flow allows reference-etl to remain a passive executor that does not need to know about the reference-api's internal state.

### reference-etl Side: HttpJobCompletionNotifier

The `HttpJobCompletionNotifier` in `Reference.Etl.Server/Services/HttpJobCompletionNotifier.cs` sends completion webhooks:

```csharp
public sealed record JobCompletionPayload(
    string ExecutionId,
    string PipelineName,
    string Status,
    DateTime StartedAt,
    DateTime CompletedAt,
    int RowsProcessed,
    string? ErrorMessage);
```

Configuration via `WebhookOptions` in `Reference.Etl.Server/Configuration/WebhookOptions.cs`:

```json
{
  "Webhooks": {
    "CompletionUrl": "http://localhost:5001/api/v1/proxy/etl/webhook/completion",
    "TimeoutSeconds": 30
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `CompletionUrl` | `""` | URL to POST completion payload. Empty disables webhooks. |
| `TimeoutSeconds` | `30` | HTTP client timeout for the webhook POST. |

When `CompletionUrl` is empty, the notifier logs `WebhookLog.NoWebhookUrlConfigured` and skips the call. When the webhook call fails, the error is logged via `WebhookLog.CompletionWebhookFailed` but does not fail the ETL execution itself (fire-and-forget with error logging).

### reference-api Side: EtlWebhookEndpoint

The `EtlWebhookEndpoint` receives completion callbacks at `POST /api/v1/proxy/etl/webhook/completion`:

**Request:**

```csharp
public sealed class EtlWebhookRequest
{
    public string ExecutionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
```

**Response:**

```csharp
public sealed class EtlWebhookResponse
{
    public bool Acknowledged { get; set; }
    public string ExecutionId { get; set; } = string.Empty;
}
```

**Validation:** If `ExecutionId` is empty or whitespace, the endpoint returns HTTP 400 with `Acknowledged = false`. On success, it returns HTTP 200 with `Acknowledged = true`.

### Webhook Flow

```
reference-etl                          reference-api
    |                                   |
    |  Pipeline execution completes     |
    |                                   |
    |  POST /api/v1/proxy/etl/          |
    |       webhook/completion          |
    |  { ExecutionId, PipelineName,     |
    |    Status, StartedAt,             |
    |    CompletedAt, RowsProcessed }   |
    |---------------------------------->|
    |                                   |  Log: EtlWebhookReceived
    |                                   |  Update execution tracking
    |                                   |  Notify SignalR clients
    |  200 OK                           |
    |  { Acknowledged: true }           |
    |<----------------------------------|
```

---

## Resiliency

### ETL Dispatch Retry (reference-scheduler)

When reference-scheduler dispatches an ETL job, it uses exponential backoff with a configurable retry policy. The `EtlDispatchService` injects `IPipelineJobClient` (the typed client from `Fdw.Services.Pipelines.Clients.Abstractions`) and wraps calls in retry logic.

Configuration via `EtlDispatchConfiguration` in `Reference.Scheduler.Server/Configuration/EtlDispatchConfiguration.cs`:

```csharp
public class EtlDispatchConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:5002";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
}
```

**Retry behavior with default settings (MaxRetries=3, RetryDelaySeconds=2):**

| Attempt | Action | Delay Before Next Attempt |
|---------|--------|--------------------------|
| 1 | Initial request | (on failure) 2 seconds |
| 2 | 1st retry | (on failure) 4 seconds |
| 3 | 2nd retry | (on failure) 8 seconds |
| 4 | 3rd retry (final) | Return failure |

The delay doubles after each attempt (`delaySeconds *= 2`). `OperationCanceledException` is always rethrown to respect cancellation tokens. All other exceptions trigger retry with structured logging via `reference-schedulerLog`. When a retry eventually succeeds, a `DispatchRetrySucceeded` log entry is emitted noting the successful attempt number.

When all retries are exhausted, the service returns `GenericResult.Failure()` with a `reference-schedulerLog.DispatchFailed` message.

### Typed Client Timeouts

Both typed client `HttpClient` instances are configured with a 30-second timeout:

```csharp
client.Timeout = TimeSpan.FromSeconds(30);
```

This prevents indefinite hangs when backend services are unresponsive.

### Typed HttpClient Pattern

All inter-service HTTP communication uses `IHttpClientFactory` with typed clients. Typed clients combine the benefits of named clients with compile-time type safety:

- **Connection pooling** -- the factory manages `HttpMessageHandler` lifetimes and DNS refresh.
- **Per-client configuration** -- each typed client (`PipelineHttpClient`, `PipelineJobHttpClient`, `ScheduleHttpClient`) has its own base address and timeout.
- **Type safety** -- consumers depend on `IPipelineClient`, `IPipelineJobClient`, or `IScheduleClient` interfaces, not raw `HttpClient`.
- **Testability** -- the typed client interfaces can be mocked directly in tests without HTTP infrastructure.
- **Railway results** -- all client methods return `IGenericResult<T>`, enabling consistent error handling at the call site.

### Framework-Level Resiliency

Beyond the reference solution patterns, FractalDataWorks provides a resiliency TypeCollection (`IResiliencyPolicy`) with built-in policies. Each policy exposes the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `MaxRetries` | `int` | Maximum retry attempts before giving up |
| `InitialDelay` | `TimeSpan` | Delay before the first retry |
| `MaxDelay` | `TimeSpan` | Upper bound on delay regardless of backoff calculation |
| `BackoffMultiplier` | `double` | Factor applied to delay after each retry (e.g., 2.0 doubles it) |
| `CircuitBreakerDuration` | `TimeSpan` | How long the circuit remains open after being tripped |
| `CircuitBreakerThreshold` | `int` | Consecutive failures required to trip the circuit |
| `ResiliencyCategory` | `ResiliencyCategory` | Category of operations this policy is designed for |

Built-in policy types include `SimpleRetryResiliencyPolicy`, `HttpClientResiliencyPolicy`, `DatabaseResiliencyPolicy`, and `CriticalResiliencyPolicy`. These are registered through the standard `AddResiliency()` extension method and consumed by `IResiliencyPipelineFactory`.

---

## SignalR Real-Time Communication

reference-api hosts three SignalR hubs for pushing real-time updates to the reference-ui:

| Hub | Route | Purpose |
|-----|-------|---------|
| `PipelineStatusHub` | `/hubs/pipelines` | ETL pipeline execution status updates |
| `CalculationHub` | `/hubs/calculations` | Calculation progress and completion events |
| `SchemaDiscoveryHub` | `/hubs/schema-discovery` | Schema introspection progress |

### SignalR Authentication

SignalR connections use JWT bearer token authentication. The reference-ui's `CalculationHubService` configures an `AccessTokenProvider` that retrieves the token from `ITokenStorageService`:

```csharp
options.AccessTokenProvider = async () =>
{
    var result = await _tokenStorage.GetAccessToken();
    return result.IsSuccess ? result.Value : null;
};
```

### Automatic Reconnection

Hub connections are configured with automatic reconnection using a progressive delay strategy:

```csharp
.WithAutomaticReconnect([
    TimeSpan.Zero,           // Immediate first retry
    TimeSpan.FromSeconds(2),
    TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(30) // Maximum backoff
])
```

Connection state transitions (reconnecting, reconnected, closed) are logged through `CalculationHubLog` for operational visibility.

---

## Structured Logging for Proxy Operations

All inter-service communication is logged through MessageLogging classes with allocated EventId ranges:

| Log Class | EventId Range | Service |
|-----------|--------------|---------|
| `ProxyLog` | 1900-1905 | reference-api proxy operations |
| `WebhookLog` | 10250-10270 | reference-etl webhook notifications |
| `reference-schedulerLog` | 8200+ | reference-scheduler dispatch operations |

Key log events:

| EventId | Level | Message |
|---------|-------|---------|
| 1900 | Information | Proxying {method} to {service}: {path} |
| 1901 | Information | Proxy response from {service}: {statusCode} |
| 1902 | Warning | Proxy call to {service} failed: {error} |
| 1903 | Error | Proxy circuit breaker open for {service} |
| 1904 | Information | ETL webhook received: execution {executionId} status {status} |
| 1905 | Warning | ETL webhook received for unknown execution {executionId} |
| 10250 | Information | Sending completion webhook for execution {executionId} |
| 10251 | Information | Completion webhook sent successfully for execution {executionId} |
| 10252 | Warning | Completion webhook failed for execution {executionId}: {error} |
| 10253 | Information | No webhook URL configured -- skipping completion notification |

---

## Related Documentation

- [12-01 Creating a Server](12-01-Creating-A-Server.md) -- Extension method reference for `AddFrameworkSerilog`, `AddConfigurationGateway`, and middleware setup
- [12-02 Deployment Guide](12-02-Deployment-Guide.md) -- Docker, environment variables, health checks
- [12-04 Security Hardening](12-04-Security-Hardening.md) -- OWASP headers, JWT, CORS, DB credential isolation
