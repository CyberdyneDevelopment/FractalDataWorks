# Headless UI Pattern


The FractalDataWorks UI layer uses a **headless architecture** that separates data logic from visual rendering. This guide explains the pattern, its components, hard rules, and how to create new providers.

## Architecture Overview

```
+-------------------------------------------------+
|  UI Framework (MudBlazor / Tailwind / Custom)   |
|  Pure rendering - HTML, CSS, layout             |
+-------------------------------------------------+
           |  RenderFragment<T>
           v
+-------------------------------------------------+
|  Headless Providers (Domain Components)         |
|  State management, loading, error handling      |
|  Per-Domain *.Components packages               |
+-------------------------------------------------+
           |  Injected via [Inject]
           v
+-------------------------------------------------+
|  API Clients                                    |
|  HTTP calls, serialization, base URL routing    |
|  Per-Domain *.Clients packages                  |
+-------------------------------------------------+
           |  HTTP (REST)
           v
+-------------------------------------------------+
|  Per-Domain Endpoints (Tier 2/3)                |
|  FastEndpoints, authorization, validation       |
|  Per-Domain *.Endpoints packages                |
+-------------------------------------------------+
           |  IOptionsMonitor / IConfigurationWriter
           v
+-------------------------------------------------+
|  ConfigurationDb                                      |
|  SQL Server, version-on-write                   |
+-------------------------------------------------+
```

## What "Headless" Means

A headless component contains all the **logic** for a feature (data fetching, state management, filtering, CRUD operations) but renders **zero HTML**. Instead, it passes its state and callbacks to child components via Blazor's `RenderFragment<T>` pattern.

This means the same logic component works with:
- **MudBlazor** (reference-aui) — Material Design components
- **Tailwind CSS** (reference-ui) — Utility-first CSS
- **WebAssembly** — Client-side standalone
- **Custom CSS** — Any other visual framework

The visual layer becomes a thin "skin" that only handles layout and styling.

## Per-Domain Package Isolation

Headless providers live in **per-domain** Razor Class Library packages. This is not just organization — it has hard dependency consequences:

| Package | Domain | Client Dependencies |
|---------|---------|---------------------|
| `Fdw.Data.Components` | Data mapping, preview, DataSet wizards | `Services.Data.Clients`, `Schema.Clients`, `Services.Connections.Clients` |
| `Fdw.Schema.Components` | Schema browser, table wizard | `Schema.Clients`, `Services.Connections.Clients`, `Services.Data.Clients` |
| `Fdw.Calculations.Components` | Calculation editor | `Web.Calculations.Clients`, `Services.Data.Clients` |
| `Fdw.Operations.Components` | Audit history, execution | `Operations.Clients` |
| `Fdw.Services.Notifications.Components` | Notification preferences | `Services.Notifications.Clients` |
| `Fdw.Services.Pipelines.Components` | Pipeline builder, schedules | `Services.Pipelines.Clients`, `UI.Pipelines.Clients` |
| `Fdw.Services.Messaging.Components` | Messages, access requests | `Services.Messaging` |

**Why this matters:** A data-only application adds `Fdw.Data.Components` and gets DataMapper and DataPreview without pulling in pipeline, notification, or scheduling dependencies. The monolithic `Fdw.UI.Components.Blazor` pattern forces all 13+ domain clients as transitive dependencies on every consumer.

**Rule:** Never reference `Fdw.UI.Components.Blazor` from domain component packages. Each domain package references only its own domain's clients.

## The Headless Provider Pattern

### Structural Contract

Every headless provider follows this exact structure:

```razor
@namespace FractalDataWorks.{Domain}.Components.Components.{Feature}
@using ...

@if (ChildContent is not null)
{
    @ChildContent(_context)
}

@code {
    // ── Parameters ──────────────────────────────────────────────────────────────
    [Parameter] public RenderFragment<{Feature}Context>? ChildContent { get; set; }

    // ── Injected services ───────────────────────────────────────────────────────
    [Inject] private {Domain}ApiClient Api { get; set; } = default!;
    [Inject] private ILogger<{Feature}Provider>? LoggerParam { get; set; }

    // ── Logger with NullLogger fallback ─────────────────────────────────────────
    private ILogger<{Feature}Provider> _logger = NullLogger<{Feature}Provider>.Instance;

    // ── State ───────────────────────────────────────────────────────────────────
    private IReadOnlyList<ItemDto> _items = [];
    private bool _isLoading;
    private string? _errorMessage;
    private {Feature}Context _context = new();
    private bool _initialized;

    // ── Lifecycle ───────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        _logger = LoggerParam ?? NullLogger<{Feature}Provider>.Instance;
        RebuildContext();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _initialized = true;
            await LoadItems();
        }
    }

    // ── Public methods (exposed via context callbacks) ───────────────────────────
    public async Task LoadItems(CancellationToken cancellationToken = default)
    {
        _isLoading = true;
        _errorMessage = null;
        RebuildContext();
        StateHasChanged();

        {Feature}ProviderLog.LoadingItems(_logger);

        var result = await Api.GetItems(cancellationToken);

        if (!result.IsSuccess)
        {
            _errorMessage = {Feature}ProviderLog.LoadItemsFailed(
                _logger, new InvalidOperationException(result.CurrentMessage ?? "API returned failure")).Message;
            _items = [];
            _isLoading = false;
            RebuildContext();
            StateHasChanged();
            return;
        }

        _items = result.Value ?? [];
        {Feature}ProviderLog.LoadedItems(_logger, _items.Count);

        _isLoading = false;
        RebuildContext();
        StateHasChanged();
    }

    // ── Private helpers ──────────────────────────────────────────────────────────
    private void RebuildContext()
    {
        _context = new {Feature}Context
        {
            Items = _items,
            IsLoading = _isLoading,
            ErrorMessage = _errorMessage,
            OnRefresh = () => LoadItems()
        };
    }
}
```

### Key Elements

1. **`RenderFragment<{Context}>`** — passes state and callbacks to the child; the context is the API surface
2. **`_context` field** — rebuilt via `RebuildContext()` before every `StateHasChanged()` call
3. **`NullLogger<T>.Instance` fallback** — `_logger = LoggerParam ?? NullLogger<T>.Instance` in `OnInitialized`
4. **`_initialized` guard** — prevents double-load on `OnAfterRenderAsync`
5. **`OperationCanceledException` catch** — always return without updating state; never log

### Context Class Contract

```csharp
namespace FractalDataWorks.{Domain}.Components.Components.{Feature};

/// <summary>
/// Immutable snapshot of <see cref="{Feature}Provider"/> state passed to consumer RenderFragments.
/// </summary>
public sealed class {Feature}Context
{
    // ── State (read-only) ────────────────────────────────────────────────────────
    public IReadOnlyList<ItemDto> Items { get; init; } = [];
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    // Why: propagated from the user's RBAC permissions resolved at provider time.
    // Skin pages check CanEdit / CanDelete before rendering mutation controls.
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }

    // ── Callbacks ────────────────────────────────────────────────────────────────
    public Func<Task> OnRefresh { get; init; } = () => Task.CompletedTask;
    public Func<string, Task> OnSelected { get; init; } = _ => Task.CompletedTask;
}
```

Context rules:
- **`sealed`** — not a base class
- **`init`** properties — set once during `RebuildContext()`
- **`IReadOnlyList<T>`** — never `List<T>` on public state; never expose internal mutable fields
- **Default no-op lambdas** — `() => Task.CompletedTask` prevents null-check spam at consumer sites
- **`CanEdit` / `CanDelete`** — propagated from the user's RBAC permissions; consumers must respect this before rendering mutations

### Usage in a Visual Component

MudBlazor skin:

```razor
<DataMapperProvider>
    <ChildContent Context="ctx">
        @if (ctx.IsLoading)
        {
            <MudProgressLinear Indeterminate="true" />
        }
        else if (ctx.ErrorMessage is not null)
        {
            <MudAlert Severity="Severity.Error">@ctx.ErrorMessage</MudAlert>
        }
        else
        {
            <MudTable Items="@ctx.Mappings">
                <RowTemplate>
                    <MudTd>@context.SourceField</MudTd>
                    <MudTd>@context.TargetField</MudTd>
                </RowTemplate>
            </MudTable>
        }
    </ChildContent>
</DataMapperProvider>
```

The same `DataMapperProvider` with Tailwind CSS:

```razor
<DataMapperProvider>
    <ChildContent Context="ctx">
        @if (ctx.IsLoading)
        {
            <div class="animate-pulse h-2 bg-blue-500 rounded"></div>
        }
        else if (ctx.ErrorMessage is not null)
        {
            <div class="rounded bg-red-900/20 p-3 text-red-400">@ctx.ErrorMessage</div>
        }
        else
        {
            <table class="min-w-full divide-y divide-gray-700">
                @foreach (var m in ctx.Mappings)
                {
                    <tr>
                        <td class="px-4 py-2">@m.SourceField</td>
                        <td class="px-4 py-2">@m.TargetField</td>
                    </tr>
                }
            </table>
        }
    </ChildContent>
</DataMapperProvider>
```

## Hard Rules for Provider Implementation

These rules are enforced by analyzers and code review. Violations must be fixed before commit.

### 1. IGenericResult — Check First, Never Wait for Exceptions

**Wrong:**
```csharp
// WRONG — waits for exception; silently discards failure if Value is null
var result = await Api.GetItems(ct);
_items = result.Value ?? [];  // FDW002/anti-pattern
```

**Wrong:**
```csharp
// WRONG — try/catch is for unexpected exceptions, not expected API failures
try
{
    var result = await Api.GetItems(ct);
    _items = result.Value!;  // throws if null — this is treating expected failures as exceptions
}
catch (Exception ex) { _errorMessage = ex.Message; }
```

**Correct:**
```csharp
// CORRECT — check result before accessing Value
var result = await Api.GetItems(ct);

if (!result.IsSuccess)
{
    _errorMessage = {Feature}ProviderLog.LoadItemsFailed(
        _logger, new InvalidOperationException(result.CurrentMessage ?? "API returned failure")).Message;
    _items = [];
    _isLoading = false;
    RebuildContext();
    StateHasChanged();
    return;
}

_items = result.Value ?? [];
```

Only wrap in `try/catch` for actual exceptions (network failures, unexpected errors). `OperationCanceledException` is always caught separately and causes a silent return:

```csharp
catch (OperationCanceledException)
{
    return;  // No state update, no log — user cancelled
}
catch (Exception ex)
{
    _errorMessage = {Feature}ProviderLog.LoadItemsFailed(_logger, ex).Message;
    _items = [];
}
```

### 2. MessageLogging — Full Coverage, Every Operation

Every provider method must be logged at the appropriate level:

| Operation | Level | Method |
|-----------|-------|--------|
| Starting an async operation | `Trace` | `{Feature}ProviderLog.Loading{Items}(_logger)` |
| Success with count | `Information` | `{Feature}ProviderLog.Loaded{Items}(_logger, count)` |
| Cache hit | `Trace` | `{Feature}ProviderLog.CacheHit(_logger, key)` |
| Cache miss | `Trace` | `{Feature}ProviderLog.CacheMiss(_logger, key)` |
| Cache invalidation | `Trace` | `{Feature}ProviderLog.CacheInvalidated(_logger, key)` |
| Unexpected-but-handled | `Warning` | `{Feature}ProviderLog.{Situation}(_logger, context)` |
| API failure / exception | `Error` | `{Feature}ProviderLog.Load{Items}Failed(_logger, ex).Message` |

**Never use raw `_logger.LogError()`, `_logger.LogInformation()`, etc.** Always use `[MessageLogging]` source-generated methods. See [07-02 MessageLogging](07-02-MessageLogging-Attribute.md).

Example log class for a provider:

```csharp
/// <summary>
/// MessageLogging for DataSetWizardProvider operations.
/// EventId range: XXXX-XXXX (from RESULTCODE-CATALOG.md)
/// </summary>
public static partial class DataSetWizardProviderLog
{
    [MessageLogging(EventId = XXXX, Level = LogLevel.Trace,
        Message = "Loading data stores")]
    public static partial IGenericMessage LoadingDataStores(ILogger logger);

    [MessageLogging(EventId = XXXX, Level = LogLevel.Information,
        Message = "Loaded {count} data stores")]
    public static partial IGenericMessage LoadedDataStores(ILogger logger, int count);

    [MessageLogging(EventId = XXXX, Level = LogLevel.Trace,
        Message = "Schema cache hit for '{key}'")]
    public static partial IGenericMessage SchemaCacheHit(ILogger logger, string key);

    [MessageLogging(EventId = XXXX, Level = LogLevel.Trace,
        Message = "Schema cache miss for '{key}' — discovering")]
    public static partial IGenericMessage SchemaCacheMiss(ILogger logger, string key);

    [MessageLogging(EventId = XXXX, Level = LogLevel.Error,
        Message = "Failed to load data stores")]
    public static partial IGenericMessage LoadDataStoresFailed(ILogger logger, Exception ex);
}
```

### 3. TypeCollections Over Enums — Every Time

**Wrong:**
```csharp
// WRONG — enum cannot be extended without touching framework source
public enum MappingType { Direct, Transform, Constant }

// WRONG — static string constants (stops FDW017, but not extensible)
public static class MappingTypeNames { public const string Direct = "Direct"; }
```

**Correct:**
```csharp
// CORRECT — TypeCollection: extensible, NotFound sentinel, O(1) ByName()
[TypeCollection(typeof(MappingTypeBase), typeof(IMappingType), typeof(MappingTypes))]
public abstract partial class MappingTypes : TypeCollectionBase<MappingTypeBase, IMappingType> { }

[TypeOption(typeof(MappingTypes), "Direct")]
public sealed class DirectMappingType : MappingTypeBase { }
```

Why this matters: When a consumer adds `AzureManagedIdentityMappingType` from their own assembly, they implement the base class and the module initializer self-registers it into the TypeCollection. No switch statements, no enum additions, no framework changes needed.

Usage:
```csharp
// O(1) lookup, returns NotFound sentinel if not registered
var mappingType = MappingTypes.ByName(mapping.MappingType);
if (mappingType == MappingTypes.NotFound)
{
    // handle unknown type — do NOT throw
}
```

Note: Static string constants (`MappingTypeNames`) are an acceptable short-term bridge while converting existing enums (tracked in YouTrack). New code must always use TypeCollections.

### 4. IReadOnlyList<T> on All Public State

**Wrong:**
```csharp
// WRONG — exposes mutable internal state through public API
public List<ConnectionDto> Connections { get; private set; } = new();
```

**Correct:**
```csharp
// CORRECT — public state is read-only
private IReadOnlyList<ConnectionDto> _connections = [];

// In context:
public IReadOnlyList<ConnectionDto> Connections { get; init; } = [];
```

Internal provider state can be `List<T>` for mutation efficiency (e.g., `_mappings = [..newMappings]`), but never exposed via context or public properties.

### 5. CancellationToken — Every Async Method

Every `async Task` method accepts `CancellationToken cancellationToken = default` and passes it to every async callee:

```csharp
public async Task LoadItems(CancellationToken cancellationToken = default)
{
    var result = await Api.GetItems(cancellationToken);
    //                              ^^^^^^^^^^^^^^^^ propagated
}
```

Exceptions: Blazor event handlers (invoked by the framework without CT), `Task.CompletedTask` returns.

### 6. NullLogger<T>.Instance — The Only Acceptable Fallback

```csharp
// CORRECT — only acceptable ?? fallback in constructors/OnInitialized
private ILogger<DataMapperProvider> _logger = NullLogger<DataMapperProvider>.Instance;

protected override void OnInitialized()
{
    _logger = LoggerParam ?? NullLogger<DataMapperProvider>.Instance;
    RebuildContext();
}
```

Never: `?? "default"`, `?? string.Empty`, `?? []`, `result.CurrentMessage ?? "An error occurred"` (except when passing to MessageLogging method as the exception message).

### 7. StringComparison — Always Explicit

```csharp
// CORRECT
string.Equals(source.DataType, target.DataType, StringComparison.OrdinalIgnoreCase)
_schemaCache.TryGetValue(cacheKey, out var schema)  // dictionary key: Ordinal by default

// WRONG
source.DataType == target.DataType
source.DataType.Equals(target.DataType)
```

Use `StringComparison.Ordinal` for internal keys and cache lookups. Use `StringComparison.OrdinalIgnoreCase` for user-facing names (connection names, table names, field names).

### 8. Explicit Failure Handling — Never Silent

Every failure path must:
1. Log via `[MessageLogging]` Error method
2. Set `_errorMessage` to the message text
3. Call `RebuildContext()` + `StateHasChanged()`
4. Return immediately

There is no "graceful degradation" in providers. If an operation fails, the error surfaces to the consumer. The consumer decides whether to show a dismissible alert or a blocking error page.

### 9. RBAC-Driven Mutation Gating

All `[ManagedConfiguration]` records are writable in 1.2.0 — there is no read-only schema
and no `IsSystem` flag. Mutation gating is the responsibility of authorization. Providers
resolve the user's RBAC permissions and surface boolean flags (`CanEdit`, `CanDelete`) on
the context. Skin pages render mutation controls only when those flags are true.

**In a provider method:**

```csharp
public async Task DeleteItem(Guid id, CancellationToken cancellationToken = default)
{
    var item = _items.FirstOrDefault(i => i.Id == id);
    if (item is null) return;

    if (!_permissions.CanDelete)
    {
        _errorMessage = {Feature}ProviderLog.NotAuthorized(_logger, id).Message;
        RebuildContext();
        StateHasChanged();
        return;
    }

    var result = await Api.DeleteItem(id, cancellationToken);
    // ... rest of handler
}
```

**In a page (visual skin):**

```razor
@foreach (var item in ctx.Items)
{
    <tr>
        <td>@item.Name</td>
        <td>
            @if (ctx.CanEdit)
            {
                <button @onclick="() => ctx.OnEdit(item)">Edit</button>
            }
            @if (ctx.CanDelete)
            {
                <button @onclick="() => ctx.OnDelete(item.Id)">Delete</button>
            }
        </td>
    </tr>
}
```

## Protocol Components (16) — Existing Monolithic Package

These providers live in `Fdw.UI.Components.Blazor/Protocols/` and cover cross-cutting concerns. They are **not** domain-specific:

| Protocol | API Client(s) | Responsibility |
|----------|---------------|----------------|
| `ConnectionProvider` | `ConnectionApiClient`, `ConfigurationApiClient` | Connection CRUD, type browsing, connectivity testing |
| `DataStoreProvider` | `DataStoreApiClient` | DataStore CRUD, introspection, container listing |
| `DataSetProvider` | `DataSetApiClient` | DataSet CRUD, field/source management |
| `SchemaProvider` | `SchemaApiClient` | Schema discovery, graph visualization |
| `PipelineProvider` | `IPipelineClient`, `IPipelineJobClient` | Pipeline CRUD, job execution, status monitoring |
| `ScheduleProvider` | `ScheduleApiClient` | Schedule management and status |
| `UserProvider` | `UserApiClient` | User CRUD, role synchronization, search |
| `RoleProvider` | `RoleApiClient` | Role definitions, permission matrix |
| `ThemeProvider` | `ThemeApiClient` | Theme CRUD, default theme management |
| `AnalyticsProvider` | `AnalyticsApiClient` | Dashboard metrics, activity feeds |
| `DataflowProvider` | `DataflowApiClient` | Dataflow graph, impact analysis |
| `LineageProvider` | `LineageApiClient` | Data lineage traversal |
| `CalculationProvider` | `CalculationApiClient` | Calculation chain operations |
| `ConfigurationProvider` | `ConfigurationApiClient` | Configuration metadata browsing |
| `DashboardProvider` | Multiple clients | Aggregated system metrics |
| `MessageProvider` | `MessageApiClient` | Messaging, access requests |

**Do not add new providers to `Fdw.UI.Components.Blazor`.** New domain providers belong in a per-domain `*.Components` package.

## Package Relationship

```
Fdw.Web.Clients.Abstractions
  ApiClientBase.cs               — shared HTTP plumbing, auth headers, error handling
  ClientLog.cs                   — structured logging (EventIds for all client operations)
  Contracts/                     — cross-domain DTO interfaces

Fdw.UI.Components.Blazor         ← monolithic (do not add to)
  Protocols/
    ConnectionProvider.razor     — injects ConnectionApiClient

Fdw.Data.Components              ← per-domain (new)
  Components/DataMapper/DataMapperProvider.razor
  Components/DataPreview/DataPreviewProvider.razor
  Components/DataSets/DataSetWizardProvider.razor
  Components/DataSets/CalculatedDataSetProvider.razor

Fdw.Schema.Components            ← per-domain (new)
  Components/Schema/SchemaExplorerProvider.razor
  Components/Schema/TableWizardProvider.razor

... (5 more per-domain *.Components packages)

Per-Domain *.Clients Packages
  Services.Connections.Clients/ConnectionApiClient.cs
  Services.Data.Clients/DataStoreApiClient.cs
  Services.Data.Clients/DataSetApiClient.cs
  Schema.Clients/SchemaApiClient.cs
  ... (10+ more)
```

## Creating a New Domain Provider

### Step 1: Choose or Create the Package

Identify which domain package owns this feature. If none exists, create one:

```bash
# From FractalDataWorks/public/
mkdir -p src/FractalDataWorks.{Domain}.Components/Components/{Feature}
mkdir -p src/FractalDataWorks.{Domain}.Components/Logging

# Create csproj
cat > src/FractalDataWorks.{Domain}.Components/FractalDataWorks.{Domain}.Components.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="{domain clients only}" />
    <ProjectReference Include="..\Fdw.MessageLogging.Abstractions\..." />
    <ProjectReference Include="..\Fdw.MessageLogging.SourceGenerators\..."
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\Fdw.Collections.SourceGenerators\..."
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
EOF

dotnet sln add src/FractalDataWorks.{Domain}.Components/... --solution-folder /src/UI/
```

### Step 2: Create the MessageLog Class

Allocate EventIds from `RESULTCODE-CATALOG.md`. Create:

```
src/FractalDataWorks.{Domain}.Components/Logging/{Feature}ProviderLog.cs
```

Cover: loading start (Trace), loaded count (Info), cache hit/miss (Trace), failures (Error).

### Step 3: Create the Context Class

```
src/FractalDataWorks.{Domain}.Components/Components/{Feature}/{Feature}Context.cs
```

One type per file (FDW005). All properties `init`. All collections `IReadOnlyList<T>`. All callbacks `Func<..., Task>` with no-op defaults.

### Step 4: Create the Provider Razor

```
src/FractalDataWorks.{Domain}.Components/Components/{Feature}/{Feature}Provider.razor
```

Follow the structural contract above. Never add HTML to a provider.

### Step 5: Consume in a Skin Page

Pages call `ctx.OnXxx()` methods. Pages own:
- Layout and visual markup
- UI-only state (wizard step, selected tab, dialog open/close)
- Navigation (`NavigationManager`)
- Snackbar/toast triggers

Pages do **not** own:
- API clients (no `@inject DataStoreApiClient DataStoreApi`)
- Loading state (provider owns it)
- Error messages (provider owns them, page just renders them)
- Business logic

## See Also

- [13-02 Creating Consumer Packages](13-02-Creating-Consumer-Packages.md) — building consumer endpoints
- [11-01 Management UI Overview](11-01-Management-UI-Overview.md) — three reference UI implementations
- [07-02 MessageLogging Attribute](07-02-MessageLogging-Attribute.md) — source-generated log methods
- [04-01 TypeCollections Overview](04-01-Overview.md) — extensible type system
