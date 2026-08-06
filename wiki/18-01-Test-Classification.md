# Test Classification

FractalDataWorks uses xUnit v3 `[Trait]` attributes to classify tests by priority and category. This enables CI pipelines to gate releases on critical tests while allowing lower-priority tests to fail without blocking.

## Priority Levels

Tests are classified into four priority levels based on the impact of failure:

| Priority | Label | CI Gate | Description |
|----------|-------|---------|-------------|
| **P0** | `Critical` | All branches | Security, data integrity, core stability. If these fail, the system is unsafe or corrupt. |
| **P1** | `High` | release/*, master | Core functionality and contracts. If these fail, major features are broken. |
| **P2** | `Normal` | None (tracked) | Feature behavior. If these fail, specific features are degraded but the system is stable. |
| **P3** | `Low` | None (tracked) | Polish, edge cases, convenience. Failures have minimal user impact. |

### P0 - Critical

Tests that protect against **security vulnerabilities, data corruption, or system instability**. A P0 failure means the release is unsafe.

| Domain | What to Classify P0 |
|--------|---------------------|
| Authentication | Token generation, validation, expiration, revocation |
| Authorization | Permission checks, role enforcement, policy evaluation |
| Input validation | SQL injection prevention, XSS prevention, path traversal |
| Credential handling | Secret masking, connection string sanitization |
| Configuration loading | `MsSqlConfigurationSource` correctness, `IsCurrent`/`IsDeleted` filtering |
| TypeCollection registry | `All()`, `ById()`, `ByName()` correctness, FrozenDictionary integrity |
| ServiceType registration | Three-phase DI (Configure/Register/Initialize) lifecycle |
| GenericResult pattern | `Success`/`Failure` propagation, message attachment |
| Data access | Query builder correctness, parameter binding, SQL generation |

### P1 - High

Tests that verify **core business logic and API contracts**. A P1 failure means a major feature doesn't work, but the system isn't unsafe.

| Domain | What to Classify P1 |
|--------|---------------------|
| API endpoints | Request/response contracts, status codes, error shapes |
| Pipeline execution | ETL trigger, status tracking, completion |
| Schema import | Table/column discovery, type mapping |
| DataGateway | Query execution, result mapping, federation |
| Calculation chain | Dependency resolution, execution ordering, result aggregation |
| Configuration writers | CRUD operations, version-on-write, child table handling |
| MessageLogging | Source generator output, EventId/Code format, parameter binding |
| Source generators | Collections, Registration, Configuration generator correctness |

### P2 - Normal

Tests that verify **feature-level behavior**. A P2 failure means a specific feature is degraded but the overall system remains functional.

| Domain | What to Classify P2 |
|--------|---------------------|
| Scheduling | Cron/interval/once trigger evaluation, schedule CRUD |
| Data quality | Rule evaluation, check execution, result aggregation |
| Catalog | Search, glossary terms, annotations |
| Promotion | Environment comparison, approval workflow |
| Rate limiting | Throttle enforcement, policy configuration |
| Resiliency | Retry policies, circuit breaker behavior |
| OpenAPI/OData | Schema generation, query translation |
| Client packages | HTTP serialization, DI registration extensions |

### P3 - Low

Tests that verify **polish, edge cases, and convenience features**. A P3 failure has minimal user impact and can be deferred.

| Domain | What to Classify P3 |
|--------|---------------------|
| Theme/styling | CSS variable generation, color parsing |
| Analytics/profiling | Metric collection, dashboard data |
| SignalR notifications | Broadcast formatting, connection lifecycle |
| UI components | Blazor rendering, component state |
| Formatting | Display formatting, string helpers |
| Documentation validation | Wiki link integrity, code example correctness |
| Performance benchmarks | Throughput, latency (informational only) |

## Category Traits

In addition to priority, tests are tagged by functional category for selective filtering:

| Category | Covers |
|----------|--------|
| `Security` | Authentication, authorization, input validation, credential handling, sanitization |
| `DataIntegrity` | Data access, configuration loading, query building, transactions |
| `CoreFramework` | TypeCollections, ServiceTypes, GenericResult, MessageLogging, source generators |
| `Api` | Endpoint behavior, HTTP clients, serialization, FastEndpoints |
| `Etl` | Pipelines, transforms, row sources, data quality |
| `Scheduling` | Triggers, schedules, dispatch |
| `Configuration` | ManagedConfiguration, IOptions binding, config writers |
| `Ui` | Blazor components, themes, SignalR |
| `SourceGen` | Source generators, analyzers, code fixes |

## Usage

### Applying Traits

```csharp
[Fact]
[Trait("Priority", "P0")]
[Trait("Category", "Security")]
public void ValidateTokenRejectsExpiredJwt()
{
    // ...
}

[Fact]
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public void GetConnectionsReturnsAllConfigured()
{
    // ...
}

[Fact]
[Trait("Priority", "P2")]
[Trait("Category", "Scheduling")]
public void CronTriggerCalculatesNextFireTime()
{
    // ...
}

[Fact]
[Trait("Priority", "P3")]
[Trait("Category", "Ui")]
public void ThemeColorParsesHexValues()
{
    // ...
}
```

### Combining Priority and Category

A test can have multiple categories but should have exactly one priority:

```csharp
[Fact]
[Trait("Priority", "P0")]
[Trait("Category", "Security")]
[Trait("Category", "DataIntegrity")]
public void ConnectionStringNeverAppearsInLogOutput()
{
    // ...
}
```

### CI Filtering

```bash
# Run only critical tests (fast gate)
dotnet test --filter "Priority=P0"

# Run critical + high (release gate)
dotnet test --filter "Priority=P0|Priority=P1"

# Run everything except low priority
dotnet test --filter "Priority!=P3"

# Run all security tests regardless of priority
dotnet test --filter "Category=Security"

# Combine: critical security tests only
dotnet test --filter "Priority=P0&Category=Security"

# Run full suite (default - no filter)
dotnet test
```

## CI Pipeline Integration

The pipeline uses priority traits to enforce different gates per branch:

| Branch | Gate | Filter | `allow_failure` |
|--------|------|--------|-----------------|
| `develop` | Full suite | (none) | `true` |
| `release/*` | P0 + P1 must pass | `Priority=P0\|Priority=P1` | `false` |
| `master` | P0 + P1 must pass | `Priority=P0\|Priority=P1` | `false` |

Tests without a `Priority` trait are treated as **P2 (Normal)** by default - they run in the full suite but don't gate releases.

## Decision Guide

When assigning priority to a new test, ask:

```
Does this test protect against:
├── Security vulnerability? ──────────────────── P0
├── Data corruption or loss? ─────────────────── P0
├── System crash or unavailability? ──────────── P0
├── Core framework contract violation? ────────── P0
├── Major feature not working? ───────────────── P1
├── API contract broken? ─────────────────────── P1
├── Source generator producing wrong code? ────── P1
├── Specific feature degraded? ───────────────── P2
├── Integration with external system broken? ──── P2
├── Cosmetic or convenience issue? ───────────── P3
└── Edge case with minimal user impact? ──────── P3
```

**When in doubt, classify higher.** It's better to over-protect than under-protect. You can always downgrade a test later after observing its failure patterns.

## Unclassified Tests

Tests without `[Trait("Priority", "...")]` are considered **unclassified**. They:

- Run in all test suites (no filtering excludes them)
- Do NOT gate releases (the CI filter explicitly selects P0|P1)
- Should be classified during regular maintenance

To find unclassified tests:

```bash
# Find test methods without Priority trait in a project
grep -rn "\[Fact\]\|[Theory\]" tests/ --include="*.cs" -l | \
  xargs grep -L "Priority"
```
