# Fdw.Services.RateLimiting.Abstractions

The rate-limiting contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IRateLimitAlgorithm` | interface | Interface for rate limiting algorithms. |
| `IRateLimitPolicy` | interface | Interface defining the contract for rate limit policy options. Rate limit policies define request… |

## Base types (3)

| Type | Kind | Purpose |
|---|---|---|
| `RateLimitAlgorithmBase` | class | Base class for rate limiting algorithms. |
| `RateLimitAlgorithms` | class | TypeCollection for rate limiting algorithms. |
| `RateLimitPolicyBase` | class | Base class for rate limit policy implementations. Provides the common structure for all rate limit… |

## Models and supporting types (9)

| Type | Kind | Purpose |
|---|---|---|
| `AdminRateLimitPolicy` | class | Rate limit policy for administrative API access. Provides the highest limits and most permissive… |
| `AuthenticatedRateLimitPolicy` | class | Rate limit policy for authenticated API users. Provides higher limits than standard access as a reward… |
| `ConcurrencyRateLimitAlgorithm` | class | Concurrency limiter that restricts the number of simultaneous active requests. Unlike time-based… |
| `FixedWindowRateLimitAlgorithm` | class | Fixed window algorithm that counts requests within discrete time windows. Simple and memory-efficient,… |
| `PremiumRateLimitPolicy` | class | Rate limit policy for premium tier API users. Provides significantly higher limits and advanced features… |
| `RateLimitPolicies` | class | Collection of all rate limit policy types. Provides O(1) lookup by Id and Name through source-generated… |
| `SlidingWindowRateLimitAlgorithm` | class | Sliding window algorithm that smoothly distributes the request limit across time. Uses weighted averages… |
| `StandardRateLimitPolicy` | class | Rate limit policy for standard unauthenticated API access. Provides conservative limits to protect the… |
| `TokenBucketRateLimitAlgorithm` | class | Token bucket algorithm that replenishes tokens at a steady rate. Allows controlled bursts while… |

## Installation

```bash
dotnet add package Fdw.Services.RateLimiting.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
