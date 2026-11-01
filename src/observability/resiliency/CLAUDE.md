# The resiliency domain

Resiliency and rate limiting: retry, Polly, primary/backup, retry-notify and the rate limiter.

This folder, `public/src/observability/resiliency/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/observability/resiliency/`.

## Projects

fractaldataworks:

- `Fdw.Services.RateLimiting`: Rate limiting services for Fdw with ASP.NET Core middleware integration.
- `Fdw.Services.RateLimiting.Abstractions`
- `Fdw.Services.Resiliency`: Resiliency services for Fdw with Polly integration for retry and circuit breaker patterns.
- `Fdw.Services.Resiliency.Abstractions`
- `Fdw.Services.Resiliency.Polly`: PollyRetry resiliency strategy — retries with backoff, optional circuit-breaker, and timeout.
- `Fdw.Services.Resiliency.PrimaryBackup`: PrimaryBackup resiliency strategy — on primary failure, route to backup data set and schedule refresh.
- `Fdw.Services.Resiliency.RetryNotify`: RetryNotify resiliency strategy — N retries with backoff, then notify on terminal failure.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IResiliencyPolicy` (Fdw.Services.Resiliency.Abstractions)
- `IRateLimitPolicy` (Fdw.Services.RateLimiting.Abstractions)
- `RateLimitPolicyBase` (Fdw.Services.RateLimiting.Abstractions)
- `ResiliencyPolicyBase` (Fdw.Services.Resiliency.Abstractions)
- `IResiliencyExecutionContext` (Fdw.Services.Resiliency.Abstractions)
- `IResiliencyCategory` (Fdw.Services.Resiliency.Abstractions)
- `IResiliencyExecutor` (Fdw.Services.Resiliency.Abstractions)
- `RateLimitAlgorithmBase` (Fdw.Services.RateLimiting.Abstractions)
- `ResiliencyCategoryBase` (Fdw.Services.Resiliency.Abstractions)
- `RateLimitPartitioningBase` (Fdw.Services.RateLimiting.Abstractions)
- `IResiliencyType` (Fdw.Services.Resiliency.Abstractions)
- `IRateLimitAlgorithm` (Fdw.Services.RateLimiting.Abstractions)
- `IResiliencyPolicyProvider` (Fdw.Services.Resiliency.Abstractions)
- `IRateLimitPartitioning` (Fdw.Services.RateLimiting.Abstractions)
- `IEffectiveResiliencyResolver` (Fdw.Services.Resiliency.Abstractions)
- `IResiliencyConfiguration` (Fdw.Services.Resiliency.Abstractions)
- `IResiliencyClient` (Fdw.Services.Resiliency.Abstractions)

## Rules that matter here

- No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/observability/resiliency/Fdw.Services.RateLimiting.Abstractions.Tests/Fdw.Services.RateLimiting.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/observability/resiliency/Fdw.Services.RateLimiting.Tests/Fdw.Services.RateLimiting.Tests.csproj -c Release`
- `dotnet test public/tests/observability/resiliency/Fdw.Services.Resiliency.Abstractions.Tests/Fdw.Services.Resiliency.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/observability/resiliency/Fdw.Services.Resiliency.Polly.Tests/Fdw.Services.Resiliency.Polly.Tests.csproj -c Release`
- `dotnet test public/tests/observability/resiliency/Fdw.Services.Resiliency.Tests/Fdw.Services.Resiliency.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (11), `collections` (9), `data` (3), `results` (3), `abstractions` (2), `configuration` (2), `notifications` (1), `scheduling` (1), `security` (1), `services` (1).
Depended on by: `configuration` (1), `pipelines` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-rsl`.
