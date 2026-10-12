# ADR-001: WASM Compatibility Assessment for Calculations Engine

**Status:** Accepted (HISTORICAL — see note below)
**Date:** 2025-01-10
**Decision Makers:** Architecture Team

> **Verification note (1.3):** the package names in §1 below predate the
> Calculations restructure. The original `Fdw.Calculations.Abstractions`
> no longer exists as a standalone package; aggregation/transformation
> implementations now live under `Fdw.Services.Transformations.*`
> (e.g. `Services.Transformations.Aggregation`, `Services.Transformations.Pivot`),
> and the entity-level calculation engine lives in
> `Fdw.Services.Calculations`. The architectural decision
> ("hybrid client/server execution") still stands; the per-package table
> below should be re-evaluated against the current layout before any
> WASM work is restarted.

## Context

Evaluate whether FractalDataWorks Calculations can run in Blazor WebAssembly for client-side calculation execution. The goal is to determine if calculation logic can execute in-browser to reduce server round-trips and improve perceived performance for interactive dashboards.

## Decision Criteria

| Metric | Target | Blocker Threshold |
|--------|--------|-------------------|
| WASM bundle size (compressed) | <3MB | >10MB |
| Cold start time (broadband) | <2s | >5s |
| Calculation (1000 rows) | <500ms | >2s |
| Source generator compatibility | Works | Fails |

---

## Assessment Results

### 1. Target Framework Analysis

| Package | Target Framework | WASM Compatible |
|---------|-----------------|-----------------|
| Fdw.Calculations.Abstractions | netstandard2.0 | **PASS** |
| Fdw.Calculations | netstandard2.0 | **PASS** |
| Fdw.Calculations.Aggregations | net10.0 | **FAIL** |
| Fdw.Results | netstandard2.0 | **PASS** |
| Fdw.Collections | netstandard2.0 | **PASS** |
| Fdw.Data.Abstractions | netstandard2.0 | **PASS** |
| Fdw.Services.Transformations | net10.0 | **FAIL** |

**Evidence:** Project file analysis shows `.Aggregations` and `.Transformations` target `net10.0` only.

**Verdict: PARTIAL PASS** - Core calculation abstractions are compatible; implementation packages are not.

---

### 2. Bundle Size Estimation

**Source LOC Analysis:**

| Package | Lines of Code |
|---------|---------------|
| Calculations.Abstractions | 1,300 |
| Calculations | 287 |
| Results | 570 |
| Collections | 1,430 |
| Data.Abstractions | 3,935 |
| Commands.Data.Abstractions | 2,886 |
| Services.Data.Abstractions | 764 |
| **Total** | **~11,172** |

**Estimated Bundle Size:**
- IL compilation: ~10-15 bytes/LOC = ~110-170KB
- Brotli compression ratio: ~5x
- Estimated compressed size: **~25-35KB** for calculation packages

**Additional WASM runtime overhead:** ~2-3MB (Blazor runtime + BCL)

**Total Estimated Bundle:** ~2.5-3.0MB (compressed)

**Verdict: PASS** - Within 3MB target, but at the margin.

---

### 3. Source Generator Compatibility

**Analysis:**
- `Collections.SourceGenerators` targets `netstandard2.0`
- Source generators execute at **compile time**, not runtime
- Generated code is standard C# with no WASM-incompatible patterns
- TypeCollection attributes use `typeof()` which is compile-time metadata, not reflection

**Evidence:**
```csharp
// All typeof() usages are in attributes - compile-time only
[TypeCollection(typeof(CalculationTypeBase), typeof(ICalculationType), typeof(CalculationTypes))]
[TypeOption(typeof(CalculationTypes), "Sum")]
```

**Verdict: PASS** - Source generators work normally; generated code is WASM-compatible.

---

### 4. Runtime Reflection Analysis

**Search Results:**
- `GetType()` - Found in `CalculationTransformationProvider.cs:268` (Services.Transformations)
- `Assembly.Load` - Not found
- `Activator.CreateInstance` - Not found
- `Type.GetType(string)` - Not found

**Evidence:** The reflection is in `Services.Transformations` which already targets net10.0, so this is a secondary concern. Core calculation packages have no runtime reflection.

**Verdict: PASS** - Core packages are reflection-free.

---

### 5. Unsafe/P-Invoke/Marshal Analysis

**Search Results:**
- `unsafe` blocks - Not found
- `DllImport` - Not found
- `Marshal.` - Not found
- `Process.` - Not found
- `Thread.` - Not found

**Verdict: PASS** - No native interop dependencies.

---

### 6. API Dependencies Analysis

**Critical Finding:** Core calculation logic depends on `IDataGateway` for data access:

```
Calculations.Abstractions
  └── Services.Data.Abstractions (IDataGateway interface)
        └── Commands.Data.Abstractions
              └── Data.Abstractions
```

**Impact:**
- Calculations require data from external sources
- WASM cannot directly access databases
- Would require API proxy pattern for data access

**Verdict: BLOCKER** - Calculations cannot run standalone in WASM without data access layer.

---

## Summary Matrix

| Criterion | Result | Evidence |
|-----------|--------|----------|
| Bundle Size (<3MB) | **PASS** | ~2.5-3.0MB estimated |
| Cold Start (<2s) | **LIKELY PASS** | Small bundle, standard Blazor startup |
| Calculation Speed (<500ms) | **UNTESTED** | No benchmark data |
| Source Generators | **PASS** | Compile-time only, generated code is compatible |
| Target Frameworks | **PARTIAL** | Abstractions pass, implementations fail |
| Runtime Reflection | **PASS** | No reflection in core packages |
| Native Interop | **PASS** | No unsafe/P-Invoke/Marshal |
| Data Access | **BLOCKER** | Requires IDataGateway proxy |

---

## Recommendation: **PROCEED WITH HYBRID ARCHITECTURE**

### Rationale

1. **Pure WASM is not viable** - Calculation execution requires data access via `IDataGateway`
2. **Abstractions are WASM-ready** - Type definitions, interfaces, and DTOs can run in WASM
3. **Server-side execution required** - Actual calculation logic must remain server-side

### Recommended Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor WASM Client                       │
├─────────────────────────────────────────────────────────────┤
│  • UI Components (MudBlazor)                                │
│  • Calculation Request DTOs                                 │
│  • Result Display/Formatting                                │
│  • Client-side validation                                   │
│  • Offline calculation queue (optional)                     │
└─────────────────────────────────────────────────────────────┘
                            │ HTTP API
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Reference.Api (Server)                   │
├─────────────────────────────────────────────────────────────┤
│  • Calculation Endpoints                                    │
│  • IDataGateway (data access)                               │
│  • Calculation Engine execution                             │
│  • Caching layer                                            │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Path

1. **Create `Fdw.Calculations.Contracts`** (netstandard2.0)
   - Request/Response DTOs
   - Calculation definition models
   - Shared between client and server

2. **Add Calculation API endpoints** to Reference.Api
   - POST `/calculations/execute` - Run calculation
   - GET `/calculations/definitions` - List available calculations
   - POST `/calculations/preview` - Preview calculation on sample data

3. **Client-side calculation builder**
   - Drag-drop calculation designer
   - Real-time validation
   - Server preview on demand

4. **Future: Selective WASM execution**
   - Simple aggregations (Sum, Count, Average) could run client-side
   - Complex calculations remain server-side
   - Would require creating `Calculations.Wasm` package

---

## Estimated Implementation Effort

| Task | Effort |
|------|--------|
| Create Calculations.Contracts package | 2-3 days |
| Calculation API endpoints | 3-4 days |
| Client calculation builder UI | 5-7 days |
| Integration testing | 2-3 days |
| **Total** | **12-17 days** |

---

## Decision

**Accept hybrid architecture.** Pure WASM execution is blocked by data access requirements. The hybrid approach provides:

- Immediate implementation path
- No framework changes required
- Future option for selective client-side execution
- Clean separation of concerns

## Consequences

### Positive
- Works with existing architecture
- No breaking changes to calculation engine
- Server-side caching benefits all clients
- Complex calculations remain performant

### Negative
- Every calculation requires server round-trip
- Offline mode not possible without significant work
- Perceived latency for interactive dashboards

### Mitigations
- Implement aggressive caching
- Use SignalR for real-time updates
- Pre-compute common calculations
- Consider WebWorker for non-blocking UI

---

## References

- [Blazor WASM Supported APIs](https://docs.microsoft.com/aspnet/core/blazor/webassembly-lazy-load-assemblies)
- [.NET WASM AOT Limitations](https://docs.microsoft.com/dotnet/core/deploying/native-aot)
- [FractalDataWorks TypeCollections Wiki](../wiki/04-01-Overview.md)
