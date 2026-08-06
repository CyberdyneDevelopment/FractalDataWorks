# Wizard TypeCollection Architecture

**Status (verified 1.3): UNIMPLEMENTED DESIGN.** The actual wizard
infrastructure in `Fdw.UI.Wizard` is a `WizardCore<TContext>`
state-machine with `WizardProviderBase<TContext>` Blazor host (see
`public/src/Fdw.UI.Wizard/`). Per-domain providers
(`DataSetWizardProvider`, `DataStoreEditorProvider`, `ConnectionWizard`, etc.)
subclass `WizardProviderBase` and drive that core directly — they do **not**
go through `WizardStepBase`, `DataStoreWizardSteps`/`WizardSteps`
MutableTypeCollections, a `WizardHost` component, or per-step
`ComponentType` dispatch as proposed in this document.

The TypeCollection-driven dispatch model described below remains a future
proposal; none of the per-domain `WizardSteps` collections, the
`WizardStepBase<TWizard>` generic base, or the `FdwDataStoreWizard.razor`
host component referenced here exist in the codebase.

---

Design document for the FDW wizard system: TypeCollection-driven, conditional-free UI component dispatch across the FDW package hierarchy.

---

## 1. Architecture Overview

The wizard system uses TypeCollections as a dispatch mechanism so that selecting a type from a dropdown automatically resolves the correct UI components without any conditional logic (`switch`, `if-else`, `is` checks). Each domain (Connection, DataStore, DataSet, Pipeline, Schedule) gets its own wizard with its own TypeCollection of steps. Implementation-specific packages (e.g., MsSql, PostgreSql, REST) register their own wizard steps into MutableTypeCollections, which are discovered at startup via module initialization -- the same mechanism used by `DataStoreTypes`, `DataSetTypes`, and `ConnectionTypes` today.

### Core Principle

> When the user selects "MsSql" from a DataStore type dropdown, the wizard does not check `if (type == "MsSql")`. Instead, it calls `DataStoreWizardSteps.All()`, finds the steps registered for that type, and renders each step's `ComponentType` directly. The type selection IS the dispatch.

### Relationship to Existing Patterns

| Existing Pattern | Wizard Equivalent |
|---|---|
| `DataStoreTypes` (MutableTypeCollection, cross-assembly) | `DataStoreWizardSteps` (MutableTypeCollection, cross-assembly) |
| `MsSqlDataStoreType` registered via module init | `MsSqlDataStoreWizardSteps` registered via module init |
| `ConnectionWizardSteps` (TypeCollection, single-assembly) | Stays as-is; connection wizard steps are domain-generic |
| `MsSqlAuthenticationTypes` (TypeCollection with component metadata) | Each wizard step carries its own `ComponentType` for self-rendering |

---

## 2. Package Layout

The wizard system spans three tiers of the FDW package hierarchy, mirroring the existing service domain pattern (`Abstractions` -> core -> implementation).

### Tier 1: FDW Core (Framework Package)

**Package:** `Fdw.UI.Components.Blazor`

Contains the base wizard infrastructure that all wizards share:

- `IWizardStep` interface (already exists)
- `WizardStepBase` base class (already exists)
- `IWizardContext` interface -- base contract for all wizard contexts
- `WizardHost` component -- generic wizard host that renders steps from any TypeCollection
- Domain-specific wizard base types (e.g., `DataStoreWizardStepBase`, `DataSetWizardStepBase`)
- Domain-specific MutableTypeCollections (e.g., `DataStoreWizardSteps`, `DataSetWizardSteps`)
- Domain-specific wizard context classes
- Domain-specific headless wizard components (e.g., `FdwDataStoreWizard.razor`)

### Tier 2: Domain UI Packages (Optional Domain-Specific)

**Packages:** `Fdw.UI.Components.Blazor.MsSql`, `Fdw.UI.Components.Blazor.PostgreSql`, etc.

These packages do NOT exist yet. They would contain implementation-specific wizard steps and their Blazor components:

- Concrete `WizardStepBase` subclasses (e.g., `MsSqlConfigureDataStoreStep`)
- Blazor `.razor` components for each step (e.g., `MsSqlConfigureDataStoreStepComponent.razor`)
- Registration into the parent MutableTypeCollection via `[TypeOption]`

### Tier 3: Domain Solutions (Reference Solutions)

**Packages:** `ManagementUI`, `ApiSolution` entry points

The entry-point executable triggers module initialization, which discovers all `[TypeOption]` types across referenced assemblies and registers them into the appropriate MutableTypeCollections. No manual registration code is needed in the entry point beyond referencing the correct packages.

### Package Dependency Flow

```
Fdw.UI.Components.Blazor          (base types, MutableTypeCollections)
    ^
    |
Fdw.UI.Components.Blazor.MsSql    (MsSql wizard steps + components)
Fdw.UI.Components.Blazor.Postgres  (PostgreSql wizard steps + components)
Fdw.UI.Components.Blazor.Rest      (REST wizard steps + components)
    ^
    |
ManagementUI (entry point)                      (module init discovers all TypeOptions)
```

---

## 3. Type Hierarchy

### 3.1 Base Wizard Step (Already Exists)

The existing `WizardStepBase` and `IWizardStep` provide the foundation. Each step carries:

- `Id` (int) -- unique within its TypeCollection
- `Name` (string) -- human-readable step name
- `CanMoveForwardTo` / `CanGoBackTo` -- navigation graph edges
- `ComponentType` (Type?) -- the Blazor component that renders this step

### 3.2 Generic Wizard Step Base

To support domain-specific wizards where the context type varies, introduce a generic layer:

```csharp
// In Fdw.UI.Components.Blazor/Wizards/

/// <summary>
/// Base class for wizard steps scoped to a specific wizard type.
/// The TWizard type parameter enables TypeCollection separation --
/// DataStore wizard steps and Pipeline wizard steps are in different collections
/// even though they share the same base navigation and rendering infrastructure.
/// </summary>
public abstract class WizardStepBase<TWizard> : WizardStepBase
    where TWizard : class
{
    protected WizardStepBase()
        : base()
    {
    }

    protected WizardStepBase(
        int id,
        string name,
        IReadOnlyList<int> canMoveForwardTo,
        IReadOnlyList<int> canGoBackTo,
        Type? componentType)
        : base(id, name, canMoveForwardTo, canGoBackTo, componentType)
    {
    }
}
```

### 3.3 Domain-Specific Step Bases

Each domain wizard gets its own step base. The type parameter is the domain's primary type (the thing being created/configured by the wizard):

```csharp
// DataStore wizard steps
public abstract class DataStoreWizardStepBase
    : WizardStepBase<IDataStore>
{
    protected DataStoreWizardStepBase() : base() { }

    protected DataStoreWizardStepBase(
        int id, string name,
        IReadOnlyList<int> canMoveForwardTo,
        IReadOnlyList<int> canGoBackTo,
        Type? componentType)
        : base(id, name, canMoveForwardTo, canGoBackTo, componentType) { }

    /// <summary>
    /// The DataStore type this step applies to (e.g., "MsSql", "Rest").
    /// Null means this step applies to ALL DataStore types (shared step).
    /// </summary>
    public abstract string? DataStoreType { get; }
}
```

### 3.4 Per-Domain TypeCollections

Each wizard domain gets its own MutableTypeCollection:

| Domain | TypeCollection Class | Step Base | TWizard |
|---|---|---|---|
| Connection | `ConnectionWizardSteps` (exists, stays TypeCollection) | `WizardStepBase` | N/A (non-generic, domain-agnostic) |
| DataStore | `DataStoreWizardSteps` | `DataStoreWizardStepBase` | `IDataStore` |
| DataSet | `DataSetWizardSteps` | `DataSetWizardStepBase` | `IDataSet` |
| Pipeline | `PipelineWizardSteps` | `PipelineWizardStepBase` | `IGenericPipeline` |
| Schedule | `ScheduleWizardSteps` | `ScheduleWizardStepBase` | `ISchedule` |

---

## 4. DataStoreWizard\<T\> Design

### 4.1 The DataStoreWizard Pattern

The `DataStoreWizard<TDataStore>` concept means: when the user selects a DataStore type from a dropdown, the wizard resolves the correct sequence of steps for that type. No conditionals. The dropdown is populated from `DataStoreTypes.All()`, and when a selection is made, the wizard filters `DataStoreWizardSteps.All()` to find steps matching that type.

```csharp
// Conceptual flow (not literal code):
// 1. User sees dropdown populated by DataStoreTypes.All()
// 2. User selects "MsSql"
// 3. Wizard queries DataStoreWizardSteps.All()
//    - Finds shared steps (DataStoreType == null): "Name", "Review"
//    - Finds MsSql steps (DataStoreType == "MsSql"): "Configure MsSql", "Test Connection"
// 4. Steps are ordered by Id and rendered sequentially
// 5. Each step's ComponentType renders itself -- no switch/case anywhere
```

### 4.2 Step Resolution

The wizard host resolves steps for the selected DataStore type:

```csharp
public static IReadOnlyList<DataStoreWizardStepBase> GetStepsForType(string dataStoreTypeName)
{
    return DataStoreWizardSteps.All()
        .Cast<DataStoreWizardStepBase>()
        .Where(s => s.DataStoreType is null
            || string.Equals(s.DataStoreType, dataStoreTypeName, StringComparison.OrdinalIgnoreCase))
        .OrderBy(s => s.Id)
        .ToList();
}
```

### 4.3 Shared vs Type-Specific Steps

Steps with `DataStoreType == null` are shared across all DataStore types. Steps with a specific `DataStoreType` only appear when that type is selected.

**Shared steps** (in `Fdw.UI.Components.Blazor`):

| Id | Name | ComponentType |
|---|---|---|
| 1 | SelectType | `SelectDataStoreTypeComponent` |
| 100 | Review | `ReviewDataStoreComponent` |
| 101 | Save | `SaveDataStoreComponent` |

**MsSql-specific steps** (in `Fdw.UI.Components.Blazor.MsSql`):

| Id | Name | DataStoreType | ComponentType |
|---|---|---|---|
| 10 | ConfigureConnection | "MsSql" | `MsSqlConfigureConnectionComponent` |
| 20 | ConfigureSchema | "MsSql" | `MsSqlConfigureSchemaComponent` |
| 30 | TestConnection | "MsSql" | `MsSqlTestConnectionComponent` |

**REST-specific steps** (in `Fdw.UI.Components.Blazor.Rest`):

| Id | Name | DataStoreType | ComponentType |
|---|---|---|---|
| 10 | ConfigureEndpoint | "Rest" | `RestConfigureEndpointComponent` |
| 20 | ConfigureAuth | "Rest" | `RestConfigureAuthComponent` |
| 30 | TestEndpoint | "Rest" | `RestTestEndpointComponent` |

### 4.4 Id Allocation Strategy

To prevent Id collisions between packages that don't know about each other:

| Range | Purpose |
|---|---|
| 1-9 | Shared pre-type-selection steps |
| 10-99 | Type-specific configuration steps (each type uses a sub-range) |
| 100-109 | Shared post-configuration steps (review, save) |

Within the type-specific range (10-99), each DataStore type implementation is free to define its own step Ids. Since steps are filtered by `DataStoreType` before ordering, Id collisions between different types (e.g., MsSql step 10 vs REST step 10) are harmless -- they never appear in the same step sequence.

---

## 5. Component Rendering

### 5.1 Self-Describing Steps

Each `WizardStepBase` carries a `ComponentType` property that is a `System.Type` reference to a Blazor component. The wizard host uses `DynamicComponent` to render it:

```razor
@* Inside the wizard host *@
@if (CurrentStep.ComponentType is not null)
{
    <DynamicComponent Type="@CurrentStep.ComponentType"
                      Parameters="@GetStepParameters()" />
}
```

### 5.2 Step Component Contract

Every step component receives the wizard context through a `[Parameter]` named `Context`. The context type varies by wizard domain:

```csharp
// In a MsSql DataStore wizard step component:
@code {
    [Parameter] public DataStoreWizardContext Context { get; set; } = default!;
}
```

The wizard host builds the parameter dictionary:

```csharp
private Dictionary<string, object> GetStepParameters()
{
    return new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["Context"] = _context
    };
}
```

### 5.3 Wizard Context per Domain

Each domain wizard has its own context class that carries the domain-specific state:

```csharp
public sealed class DataStoreWizardContext
{
    public DataStoreWizardStepBase CurrentStep { get; init; }
    public CreateDataStoreWithPathsRequest Model { get; init; }
    public IDataStoreType? SelectedType { get; init; }
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    public Func<Task> OnNextStep { get; init; }
    public Action OnPreviousStep { get; init; }
    public Func<Task> OnSave { get; init; }
}
```

### 5.4 Headless Component Pattern (Existing)

The existing `FdwConnectionWizard.razor` pattern is the template. The headless component:

1. Owns the state (model, current step, loading, errors)
2. Exposes a `Content` RenderFragment that receives the context
3. Manages step navigation via `Next()` / `Previous()` on the step graph
4. Handles async operations (test, save) with cancellation support

The new domain wizards follow this exact pattern but use `DynamicComponent` for step-specific rendering instead of delegating all rendering to the consumer via `RenderFragment<TContext>`.

### 5.5 Dual Rendering Strategy

The wizard supports two modes:

**Mode 1: Self-rendering steps (new pattern)**
Each step has a `ComponentType` that renders itself. The wizard host uses `DynamicComponent`. This is the primary mode for domain-specific wizards where each DataStore type brings its own UI.

**Mode 2: Consumer-controlled rendering (existing pattern)**
The `FdwConnectionWizard` pattern where the consumer provides the entire UI via `RenderFragment<TContext>`. The step's `ComponentType` is informational only. This mode remains available for simple wizards or cases where the consumer wants full control.

---

## 6. Cross-Assembly Discovery

### 6.1 The Module Initialization Pattern

This is the same pattern used by `DataStoreTypes` (MutableTypeCollection) today:

1. **Library assembly** defines a `[TypeOption]` on a class (e.g., `MsSqlDataStoreType`)
2. **Source generator** (`TypeOptionModuleInitializerGenerator`) scans all referenced assemblies at compile time
3. **Generated module initializer** in the **entry-point executable** calls `DataStoreTypes.RegisterMember(new MsSqlDataStoreType())` before any user code runs

For wizard steps, the exact same mechanism applies:

1. `Fdw.UI.Components.Blazor` defines `DataStoreWizardSteps` as a `[MutableTypeCollection]`
2. `Fdw.UI.Components.Blazor.MsSql` defines `MsSqlConfigureDataStoreStep` with `[TypeOption(typeof(DataStoreWizardSteps), "MsSqlConfigure")]`
3. When the ManagementUI compiles, the generator sees `MsSqlConfigureDataStoreStep` in a referenced assembly and generates:

```csharp
// Auto-generated in ManagementUI
[ModuleInitializer]
internal static void Initialize()
{
    DataStoreWizardSteps.RegisterMember(new MsSqlConfigureDataStoreStep());
    DataStoreWizardSteps.RegisterMember(new MsSqlTestDataStoreStep());
    // ... other steps from other referenced packages
}
```

### 6.2 RestrictToCurrentCompilation

The `ConnectionWizardSteps` TypeCollection uses `RestrictToCurrentCompilation = true` on its `[TypeOption]` attributes because connection wizard steps are defined in the same assembly as the collection. This prevents the module initializer generator from trying to re-register them.

For cross-assembly wizard TypeCollections (DataStore, DataSet, Pipeline, Schedule), `RestrictToCurrentCompilation` is NOT set on the `[TypeOption]` attributes -- the steps are in different assemblies and must be discovered via module initialization.

### 6.3 Hosting Extension Registration

As an alternative to automatic module initialization (which only works for executable assemblies), the Hosting extensions provide explicit registration. This follows the existing `DataStoreRegistrationBuilder.RegisterMsSql()` pattern:

```csharp
// In Fdw.Hosting.MsSql/Extensions/WizardRegistrationExtensions.cs
public static class WizardRegistrationExtensions
{
    public static DataStoreRegistrationBuilder RegisterMsSqlWizard(
        this DataStoreRegistrationBuilder builder)
    {
        builder.Register(() =>
        {
            DataStoreWizardSteps.RegisterMember(new MsSqlConfigureDataStoreStep());
            DataStoreWizardSteps.RegisterMember(new MsSqlSchemaDataStoreStep());
            DataStoreWizardSteps.RegisterMember(new MsSqlTestDataStoreStep());
        });
        return builder;
    }
}
```

This is a belt-and-suspenders approach: module initialization handles it automatically for executables, but explicit registration is available for test projects or scenarios where module init doesn't fire.

---

## 7. File Structure

### 7.1 FDW Core: `Fdw.UI.Components.Blazor`

```
src/Fdw.UI.Components.Blazor/
  Wizards/
    # Existing (unchanged)
    IWizardStep.cs
    WizardStepBase.cs
    ConnectionWizardSteps.cs
    ConfigureConnectionStep.cs
    TestConnectionStep.cs
    SaveConnectionStep.cs
    ConfigureConnectionStepComponent.razor
    TestConnectionStepComponent.razor
    SaveConnectionStepComponent.razor

    # New: Generic wizard step base
    WizardStepBase{TWizard}.cs

    # New: DataStore wizard
    DataStore/
      DataStoreWizardStepBase.cs
      DataStoreWizardSteps.cs                    # [MutableTypeCollection]
      DataStoreWizardContext.cs
      SelectDataStoreTypeStep.cs                  # Shared step: type dropdown
      SelectDataStoreTypeComponent.razor
      ReviewDataStoreStep.cs                      # Shared step: review
      ReviewDataStoreComponent.razor
      SaveDataStoreStep.cs                        # Shared step: save
      SaveDataStoreComponent.razor

    # New: DataSet wizard
    DataSet/
      DataSetWizardStepBase.cs
      DataSetWizardSteps.cs                       # [MutableTypeCollection]
      DataSetWizardContext.cs
      SelectDataSetTypeStep.cs
      SelectDataSetTypeComponent.razor
      ConfigureDataSetSourceStep.cs
      ConfigureDataSetSourceComponent.razor
      ReviewDataSetStep.cs
      ReviewDataSetComponent.razor
      SaveDataSetStep.cs
      SaveDataSetComponent.razor

    # New: Pipeline wizard
    Pipeline/
      PipelineWizardStepBase.cs
      PipelineWizardSteps.cs                      # [MutableTypeCollection]
      PipelineWizardContext.cs
      SelectPipelineTypeStep.cs
      SelectPipelineTypeComponent.razor
      ReviewPipelineStep.cs
      ReviewPipelineComponent.razor
      SavePipelineStep.cs
      SavePipelineComponent.razor

    # New: Schedule wizard
    Schedule/
      ScheduleWizardStepBase.cs
      ScheduleWizardSteps.cs                      # [MutableTypeCollection]
      ScheduleWizardContext.cs
      ConfigureScheduleStep.cs
      ConfigureScheduleComponent.razor
      ReviewScheduleStep.cs
      ReviewScheduleComponent.razor
      SaveScheduleStep.cs
      SaveScheduleComponent.razor

  # New: Headless wizard host components
  Components/
    DataStores/
      FdwDataStoreWizard.razor                    # Headless wizard host
    DataSets/
      FdwDataSetWizard.razor                      # Headless wizard host
    Pipelines/
      FdwPipelineWizard.razor                     # Headless wizard host
    Schedules/
      FdwScheduleWizard.razor                     # Headless wizard host
```

### 7.2 MsSql Implementation: `Fdw.UI.Components.Blazor.MsSql` (NEW PACKAGE)

```
src/Fdw.UI.Components.Blazor.MsSql/
  Fdw.UI.Components.Blazor.MsSql.csproj
  Wizards/
    DataStore/
      MsSqlConfigureDataStoreStep.cs              # [TypeOption(typeof(DataStoreWizardSteps))]
      MsSqlConfigureDataStoreComponent.razor
      MsSqlConfigureSchemaStep.cs                 # [TypeOption(typeof(DataStoreWizardSteps))]
      MsSqlConfigureSchemaComponent.razor
      MsSqlTestDataStoreStep.cs                   # [TypeOption(typeof(DataStoreWizardSteps))]
      MsSqlTestDataStoreComponent.razor
    DataSet/
      MsSqlConfigureDataSetSourceStep.cs          # [TypeOption(typeof(DataSetWizardSteps))]
      MsSqlConfigureDataSetSourceComponent.razor
      MsSqlSelectTableStep.cs                     # [TypeOption(typeof(DataSetWizardSteps))]
      MsSqlSelectTableComponent.razor
```

### 7.3 PostgreSql Implementation: `Fdw.UI.Components.Blazor.PostgreSql` (NEW PACKAGE)

```
src/Fdw.UI.Components.Blazor.PostgreSql/
  Fdw.UI.Components.Blazor.PostgreSql.csproj
  Wizards/
    DataStore/
      PostgreSqlConfigureDataStoreStep.cs
      PostgreSqlConfigureDataStoreComponent.razor
      PostgreSqlTestDataStoreStep.cs
      PostgreSqlTestDataStoreComponent.razor
```

### 7.4 REST Implementation: `Fdw.UI.Components.Blazor.Rest` (NEW PACKAGE)

```
src/Fdw.UI.Components.Blazor.Rest/
  Fdw.UI.Components.Blazor.Rest.csproj
  Wizards/
    DataStore/
      RestConfigureEndpointStep.cs
      RestConfigureEndpointComponent.razor
      RestConfigureAuthStep.cs
      RestConfigureAuthComponent.razor
      RestTestEndpointStep.cs
      RestTestEndpointComponent.razor
```

### 7.5 Hosting Extensions

```
src/Fdw.Hosting.MsSql/
  Extensions/
    DataStoreRegistrationExtensions.cs            # Existing
    WizardRegistrationExtensions.cs               # NEW: RegisterMsSqlWizard()
```

### 7.6 Reference Solution Entry Point

```
ReferenceSolutions/ManagementUI/
  # No changes needed -- module init handles registration automatically
  # If ManagementUI references Fdw.UI.Components.Blazor.MsSql,
  # the generated module initializer registers MsSql wizard steps
```

---

## 8. Example: DataStore Wizard (MsSql End-to-End)

This section walks through the complete MsSql DataStore wizard flow, from type definitions to runtime rendering.

### 8.1 Step 1: Define the MutableTypeCollection (FDW Core)

```csharp
// src/Fdw.UI.Components.Blazor/Wizards/DataStore/DataStoreWizardSteps.cs

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Blazor.Wizards.DataStore;

/// <summary>
/// MutableTypeCollection for DataStore wizard steps.
/// Supports cross-assembly registration: MsSql, PostgreSql, REST packages
/// each register their own steps via module initialization.
/// </summary>
[ExcludeFromCodeCoverage]
[MutableTypeCollection(
    typeof(DataStoreWizardStepBase),
    typeof(IWizardStep),
    typeof(DataStoreWizardSteps))]
public sealed partial class DataStoreWizardSteps
    : TypeCollectionBase<DataStoreWizardStepBase, IWizardStep>
{
}
```

### 8.2 Step 2: Define the Domain Step Base (FDW Core)

```csharp
// src/Fdw.UI.Components.Blazor/Wizards/DataStore/DataStoreWizardStepBase.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Blazor.Wizards.DataStore;

/// <summary>
/// Base class for DataStore wizard steps.
/// Each step knows which DataStore type it belongs to (null = shared across all types).
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataStoreWizardStepBase : WizardStepBase
{
    protected DataStoreWizardStepBase()
        : base()
    {
    }

    protected DataStoreWizardStepBase(
        int id,
        string name,
        IReadOnlyList<int> canMoveForwardTo,
        IReadOnlyList<int> canGoBackTo,
        Type? componentType,
        string? dataStoreType)
        : base(id, name, canMoveForwardTo, canGoBackTo, componentType)
    {
        DataStoreType = dataStoreType;
    }

    /// <summary>
    /// The DataStore type name this step applies to.
    /// Null means the step is shared across all DataStore types.
    /// Matches the Name property of the IDataStoreType (e.g., "MsSql", "Rest", "File").
    /// </summary>
    public string? DataStoreType { get; }
}
```

### 8.3 Step 3: Define Shared Steps (FDW Core)

```csharp
// src/Fdw.UI.Components.Blazor/Wizards/DataStore/SelectDataStoreTypeStep.cs

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Blazor.Wizards.DataStore;

/// <summary>
/// First step: select the DataStore type from DataStoreTypes.All().
/// Shared across all DataStore types (DataStoreType = null).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreWizardSteps), "SelectType", RestrictToCurrentCompilation = true)]
public sealed class SelectDataStoreTypeStep : DataStoreWizardStepBase
{
    public SelectDataStoreTypeStep() : base(
        id: 1,
        name: "Select Type",
        canMoveForwardTo: [10],   // Forward to first type-specific step
        canGoBackTo: [],
        componentType: typeof(SelectDataStoreTypeComponent),
        dataStoreType: null)      // Shared step
    {
    }
}
```

```csharp
// src/Fdw.UI.Components.Blazor/Wizards/DataStore/SaveDataStoreStep.cs

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Blazor.Wizards.DataStore;

/// <summary>
/// Final step: review and save the DataStore configuration.
/// Shared across all DataStore types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreWizardSteps), "Save", RestrictToCurrentCompilation = true)]
public sealed class SaveDataStoreStep : DataStoreWizardStepBase
{
    public SaveDataStoreStep() : base(
        id: 100,
        name: "Save",
        canMoveForwardTo: [],
        canGoBackTo: [30],        // Back to last type-specific step
        componentType: typeof(SaveDataStoreComponent),
        dataStoreType: null)      // Shared step
    {
    }
}
```

### 8.4 Step 4: Define MsSql-Specific Steps (Implementation Package)

```csharp
// src/Fdw.UI.Components.Blazor.MsSql/Wizards/DataStore/MsSqlConfigureDataStoreStep.cs

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Components.Blazor.Wizards.DataStore;

namespace Fdw.UI.Components.Blazor.MsSql.Wizards.DataStore;

/// <summary>
/// MsSql-specific step: configure connection string, database, authentication.
/// Only appears when the user selects "MsSql" as the DataStore type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreWizardSteps), "MsSqlConfigure")]
public sealed class MsSqlConfigureDataStoreStep : DataStoreWizardStepBase
{
    public MsSqlConfigureDataStoreStep() : base(
        id: 10,
        name: "Configure Connection",
        canMoveForwardTo: [20],
        canGoBackTo: [1],                               // Back to type selection
        componentType: typeof(MsSqlConfigureDataStoreComponent),
        dataStoreType: "MsSql")                          // MsSql only
    {
    }
}
```

```csharp
// src/Fdw.UI.Components.Blazor.MsSql/Wizards/DataStore/MsSqlConfigureSchemaStep.cs

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Components.Blazor.Wizards.DataStore;

namespace Fdw.UI.Components.Blazor.MsSql.Wizards.DataStore;

/// <summary>
/// MsSql-specific step: select database schema and configure paths.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreWizardSteps), "MsSqlSchema")]
public sealed class MsSqlConfigureSchemaStep : DataStoreWizardStepBase
{
    public MsSqlConfigureSchemaStep() : base(
        id: 20,
        name: "Configure Schema",
        canMoveForwardTo: [30],
        canGoBackTo: [10],
        componentType: typeof(MsSqlConfigureSchemaComponent),
        dataStoreType: "MsSql")
    {
    }
}
```

```csharp
// src/Fdw.UI.Components.Blazor.MsSql/Wizards/DataStore/MsSqlTestDataStoreStep.cs

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Components.Blazor.Wizards.DataStore;

namespace Fdw.UI.Components.Blazor.MsSql.Wizards.DataStore;

/// <summary>
/// MsSql-specific step: test the DataStore connection.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreWizardSteps), "MsSqlTest")]
public sealed class MsSqlTestDataStoreStep : DataStoreWizardStepBase
{
    public MsSqlTestDataStoreStep() : base(
        id: 30,
        name: "Test Connection",
        canMoveForwardTo: [100],      // Forward to shared Save step
        canGoBackTo: [20],
        componentType: typeof(MsSqlTestDataStoreComponent),
        dataStoreType: "MsSql")
    {
    }
}
```

### 8.5 Step 5: Define the Wizard Context (FDW Core)

```csharp
// src/Fdw.UI.Components.Blazor/Wizards/DataStore/DataStoreWizardContext.cs

using System;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.UI.Components.Blazor.Wizards.DataStore;

/// <summary>
/// Context for the DataStore creation wizard.
/// Passed to each step component via the "Context" parameter.
/// </summary>
public sealed class DataStoreWizardContext
{
    /// <summary>Gets the current wizard step.</summary>
    public DataStoreWizardStepBase CurrentStep { get; init; } = default!;

    /// <summary>Gets the DataStore configuration model being built.</summary>
    public CreateDataStoreWithPathsRequest Model { get; init; } = new();

    /// <summary>Gets the selected DataStore type (null until user picks one).</summary>
    public IDataStoreType? SelectedType { get; init; }

    /// <summary>Gets all available DataStore types for the selection dropdown.</summary>
    public IReadOnlyList<IDataStoreType> AvailableTypes { get; init; } = [];

    /// <summary>Gets whether an operation is in progress.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Gets the error message, if any.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets whether the wizard has completed successfully.</summary>
    public bool IsComplete { get; init; }

    /// <summary>Callback to advance to the next step.</summary>
    public Func<Task> OnNextStep { get; init; } = () => Task.CompletedTask;

    /// <summary>Callback to return to the previous step.</summary>
    public Action OnPreviousStep { get; init; } = () => { };

    /// <summary>Callback when the DataStore type is selected from the dropdown.</summary>
    public Func<IDataStoreType, Task> OnTypeSelected { get; init; } = _ => Task.CompletedTask;

    /// <summary>Callback to save the DataStore.</summary>
    public Func<Task> OnSave { get; init; } = () => Task.CompletedTask;
}
```

### 8.6 Step 6: Headless Wizard Host (FDW Core)

```razor
@* src/Fdw.UI.Components.Blazor/Components/DataStores/FdwDataStoreWizard.razor *@

@namespace Fdw.UI.Components.Blazor.Components.DataStores
@using Microsoft.AspNetCore.Components
@using Microsoft.Extensions.Logging
@using Microsoft.Extensions.Logging.Abstractions
@using System
@using System.Linq
@using System.Threading
@using System.Threading.Tasks
@using Fdw.Services.Data.Abstractions
@using Fdw.Services.Data.Clients
@using Fdw.Services.Data.Clients.Models
@using Fdw.UI.Components.Blazor.Wizards.DataStore

@implements IDisposable

@* Render the current step's component via DynamicComponent *@
@if (_context.CurrentStep.ComponentType is not null)
{
    <DynamicComponent Type="@_context.CurrentStep.ComponentType"
                      Parameters="@_stepParameters" />
}

@code {
    [Parameter] public EventCallback<DataStoreDetailDto> OnCompleted { get; set; }

    [Inject] private DataStoreApiClient DataStoreApi { get; set; } = default!;
    [Inject] private ILogger<FdwDataStoreWizard> Logger { get; set; } = default!;

    private ILogger<FdwDataStoreWizard> _logger = default!;
    private CreateDataStoreWithPathsRequest _model = new();
    private IDataStoreType? _selectedType;
    private DataStoreWizardStepBase _currentStep = default!;
    private IReadOnlyList<DataStoreWizardStepBase> _activeSteps = [];
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isComplete;
    private DataStoreWizardContext _context = default!;
    private Dictionary<string, object> _stepParameters = new(StringComparer.Ordinal);
    private CancellationTokenSource? _cts;

    protected override void OnInitialized()
    {
        _logger = Logger ?? NullLogger<FdwDataStoreWizard>.Instance;
        // Start with shared steps only (no type selected yet)
        RefreshActiveSteps();
        _currentStep = _activeSteps[0];
        UpdateContext();
    }

    private void RefreshActiveSteps()
    {
        var typeName = _selectedType?.Name;
        _activeSteps = DataStoreWizardSteps.All()
            .Cast<DataStoreWizardStepBase>()
            .Where(s => s.DataStoreType is null
                || string.Equals(s.DataStoreType, typeName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Id)
            .ToList();
    }

    private Task HandleTypeSelected(IDataStoreType type)
    {
        _selectedType = type;
        _model.StoreType = type.Name;
        RefreshActiveSteps();
        // Auto-advance to first type-specific step
        return NextStep();
    }

    private Task NextStep()
    {
        var next = _currentStep.Next(_activeSteps);
        if (next is not null)
        {
            _currentStep = (DataStoreWizardStepBase)next;
        }
        UpdateContext();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void PreviousStep()
    {
        var prev = _currentStep.Previous(_activeSteps);
        if (prev is not null)
        {
            _currentStep = (DataStoreWizardStepBase)prev;
        }
        _errorMessage = null;
        UpdateContext();
        StateHasChanged();
    }

    private async Task Save()
    {
        _isLoading = true;
        _errorMessage = null;
        UpdateContext();
        StateHasChanged();

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            var result = await DataStoreApi.CreateDataStore(_model, _cts.Token);
            if (result is not null)
            {
                _isComplete = true;
                if (OnCompleted.HasDelegate)
                {
                    await OnCompleted.InvokeAsync(result);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
            UpdateContext();
            StateHasChanged();
        }
    }

    private void UpdateContext()
    {
        _context = new DataStoreWizardContext
        {
            CurrentStep = _currentStep,
            Model = _model,
            SelectedType = _selectedType,
            AvailableTypes = DataStoreTypes.All().ToList(),
            IsLoading = _isLoading,
            ErrorMessage = _errorMessage,
            IsComplete = _isComplete,
            OnNextStep = NextStep,
            OnPreviousStep = PreviousStep,
            OnTypeSelected = HandleTypeSelected,
            OnSave = Save,
        };

        _stepParameters = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Context"] = _context
        };
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
```

### 8.7 Step 7: MsSql Step Component Example

```razor
@* src/Fdw.UI.Components.Blazor.MsSql/Wizards/DataStore/MsSqlConfigureDataStoreComponent.razor *@

@namespace Fdw.UI.Components.Blazor.MsSql.Wizards.DataStore
@using Fdw.UI.Components.Blazor.Wizards.DataStore

@* This component renders the MsSql-specific configuration form. *@
@* The consumer (ManagementUI) wraps this in MudBlazor layout. *@
@* This headless component exposes its context for the consumer to style. *@

@if (Content is not null)
{
    @Content(Context)
}

@code {
    [Parameter] public DataStoreWizardContext Context { get; set; } = default!;
    [Parameter] public RenderFragment<DataStoreWizardContext>? Content { get; set; }
}
```

### 8.8 Step 8: Module Initialization (Auto-Generated)

When ManagementUI compiles with a reference to `Fdw.UI.Components.Blazor.MsSql`, the `TypeOptionModuleInitializerGenerator` produces:

```csharp
// Auto-generated in ManagementUI assembly
using System.Runtime.CompilerServices;
using Fdw.UI.Components.Blazor.Wizards.DataStore;
using Fdw.UI.Components.Blazor.MsSql.Wizards.DataStore;

namespace ManagementUI.Generated
{
    internal static class TypeOptionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Register MsSql DataStore wizard steps
            DataStoreWizardSteps.RegisterMember(new MsSqlConfigureDataStoreStep());
            DataStoreWizardSteps.RegisterMember(new MsSqlConfigureSchemaStep());
            DataStoreWizardSteps.RegisterMember(new MsSqlTestDataStoreStep());
        }
    }
}
```

### 8.9 Runtime Flow Summary

```
1. ManagementUI starts
   -> Module initializer fires
   -> MsSql steps registered into DataStoreWizardSteps

2. User navigates to "Create DataStore" page
   -> FdwDataStoreWizard initializes
   -> DataStoreWizardSteps.All() returns shared + MsSql steps
   -> Current step = SelectDataStoreTypeStep (id: 1)

3. SelectDataStoreTypeComponent renders
   -> Shows dropdown populated by DataStoreTypes.All()
   -> Items: "MsSql", "Rest", "File", "Soap" (whatever is registered)

4. User selects "MsSql"
   -> OnTypeSelected("MsSql") fires
   -> RefreshActiveSteps() filters to shared + MsSql steps
   -> Auto-advances to MsSqlConfigureDataStoreStep (id: 10)

5. DynamicComponent renders MsSqlConfigureDataStoreComponent
   -> MsSql-specific form: server, database, auth type dropdown
   -> Auth type dropdown populated by MsSqlAuthenticationTypes.All()
   -> No conditional logic anywhere

6. User fills form, clicks Next
   -> Advances to MsSqlConfigureSchemaStep (id: 20)
   -> MsSqlConfigureSchemaComponent renders schema selection

7. User clicks Next
   -> Advances to MsSqlTestDataStoreStep (id: 30)
   -> MsSqlTestDataStoreComponent renders test button + results

8. User clicks Next
   -> Advances to SaveDataStoreStep (id: 100, shared)
   -> SaveDataStoreComponent renders review + save button

9. User clicks Save
   -> FdwDataStoreWizard.Save() calls DataStoreApi.CreateDataStore()
   -> OnCompleted fires with the new DataStore
```

---

## Appendix A: Design Decisions

### Why MutableTypeCollection instead of TypeCollection?

`TypeCollection` is compile-time-only and restricted to a single assembly. Wizard steps for MsSql, PostgreSql, REST, etc., live in different packages. `MutableTypeCollection` supports runtime registration from cross-assembly module initializers, matching the existing `DataStoreTypes` and `DataSetTypes` patterns.

### Why not generic `DataStoreWizard<TDataStore>`?

The user's original vision described `DataStoreWizard<T> where T : TDataStore`. After analysis, the generic type parameter on the wizard host itself is unnecessary because:

1. The wizard host does not need compile-time knowledge of which DataStore type is being created -- it discovers steps at runtime from the TypeCollection.
2. The `DataStoreType` property on `DataStoreWizardStepBase` provides the same filtering capability without requiring a closed generic at the host level.
3. The `WizardStepBase<TWizard>` generic base exists to provide TypeCollection separation (DataStore steps vs Pipeline steps), not to parameterize the wizard host.

The generic constraint lives on the **step base** (`WizardStepBase<TWizard>`), not on the wizard host.

### Why `DynamicComponent` instead of `RenderFragment`?

The existing `FdwConnectionWizard` uses `RenderFragment<ConnectionWizardContext>` where the consumer provides the entire UI. This works for simple wizards but breaks down when steps come from different packages. `DynamicComponent` allows each step to bring its own component type, which is resolved at runtime from the TypeCollection. The consumer can still override styling by providing a `Content` RenderFragment on the step component itself.

### Why separate UI packages per implementation?

Placing MsSql wizard components in `Fdw.UI.Components.Blazor.MsSql` rather than in `Fdw.Services.Connections.MsSql` follows the FDW principle of separating concerns. The service layer (`Services.Connections.MsSql`) has no dependency on Blazor. The UI layer (`UI.Components.Blazor.MsSql`) depends on both the service layer and Blazor, keeping the dependency graph clean.

### Why Id ranges instead of sequential Ids?

Type-specific steps are registered from different assemblies that don't know about each other. Using Id ranges (1-9 shared, 10-99 type-specific, 100+ shared) prevents collisions. Within the type-specific range, each implementation is free to use any Ids because steps are filtered by `DataStoreType` before navigation -- MsSql step 10 and REST step 10 never appear in the same step sequence.
