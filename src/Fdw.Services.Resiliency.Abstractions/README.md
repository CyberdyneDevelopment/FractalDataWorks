# Fdw.Services.Resiliency.Abstractions

The resiliency contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (8)

| Type | Kind | Purpose |
|---|---|---|
| `IEffectiveResiliencyResolver` | interface | Resolves the effective resiliency policy for a stage by walking the hierarchy: step → stage → project →… |
| `IResiliencyCategory` | interface | Interface for resiliency policy categories. |
| `IResiliencyClient` | interface | HTTP client interface for the FDW Resiliency service API. |
| `IResiliencyExecutionContext` | interface | Execution context passed into each strategy's Execute call. Provides correlation data for logging,… |
| `IResiliencyExecutor` | interface | Resolves the effective resiliency policy for a stage, loads its configuration, dispatches to the… |
| `IResiliencyPolicy` | interface | Interface defining the contract for resiliency policy options. Resiliency policies define retry, circuit… |
| `IResiliencyPolicyProvider` | interface | Provider for resiliency policy configurations. Reads server defaults and tenant-scoped policies from… |
| `IResiliencyType` | interface | TypeOption interface for pluggable resiliency strategies. Each strategy (PollyRetry, PrimaryBackup,… |

## Base types (3)

| Type | Kind | Purpose |
|---|---|---|
| `ResiliencyCategories` | class | TypeCollection for resiliency policy categories. |
| `ResiliencyCategoryBase` | class | Base class for resiliency policy categories. |
| `ResiliencyPolicyBase` | class | Base class for resiliency policy implementations. Provides the common structure for all resiliency… |

## Models and supporting types (10)

| Type | Kind | Purpose |
|---|---|---|
| `CriticalResiliencyCategory` | class | Critical operations requiring aggressive retry behavior. Used for essential operations where failure is… |
| `CriticalResiliencyPolicy` | class | Resiliency policy for critical operations requiring aggressive retry behavior. Designed for essential… |
| `DatabaseResiliencyCategory` | class | Database operations including queries, commands, and transactions. Typically uses moderate retry counts… |
| `DatabaseResiliencyPolicy` | class | Resiliency policy optimized for database operations. Designed to handle transient connection issues,… |
| `HttpClientResiliencyCategory` | class | HTTP client operations including REST API calls and web service requests. Designed for network-related… |
| `HttpClientResiliencyPolicy` | class | Resiliency policy optimized for HTTP client operations. Designed to handle network-related transient… |
| `ResiliencyPolicies` | class | Collection of all resiliency policy types. Provides O(1) lookup by Id and Name through source-generated… |
| `ResiliencyPolicyDto` | class | DTO representing a resiliency policy as returned by the Resiliency API client. |
| `SimpleResiliencyCategory` | class | General purpose simple retry for basic operations. Uses minimal retry logic with short delays for quick… |
| `SimpleRetryResiliencyPolicy` | class | Simple resiliency policy for basic operations. Designed for operations requiring minimal retry logic… |

## Installation

```bash
dotnet add package Fdw.Services.Resiliency.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Results` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
