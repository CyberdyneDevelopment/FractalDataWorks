# Fdw.Services.Etl

The ETL domain: pipelines, their operations, and the execution surface that runs them.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `EtlResultCodes` | class | TypeCollection for ETL result codes. EventId range: 8101-8199 (Services.Etl domain) |

## Options (2 declared)

| Type | Kind | Purpose |
|---|---|---|

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `EtlPipelineConfiguration` | class | The ETL-KIND typed body of PipelineConfiguration (kind "Etl"). Persisted in pipe.EtlPipeline as a… |
| `PipelineTransformAggregationConfiguration` | class | Configuration for a single aggregation within an Aggregate transform. Cascade child of… |
| `PipelineTransformCalculationConfiguration` | class | Configuration for a single computed column within a Calculate transform. Cascade child of… |
| `PipelineTransformConfiguration` | class | Database-backed configuration for a pipeline transform step. Child of EtlPipelineConfiguration… |
| `PipelineTransformFieldMappingConfiguration` | class | Configuration for a field mapping within a Map transform. |
| `PipelineTransformGroupByFieldConfiguration` | class | Configuration for a single group-by field within an Aggregate transform. Cascade child of… |
| `PipelineTransformLookupConfiguration` | class | Configuration for a single brought-across column within a Lookup transform. Cascade child of… |

## The graph pipeline type (FDW-6244)

`GraphPipelineTypeBase<TIn, TOut>` is the base of a graph implementation of the Pipeline domain (`PipelineTypes`, Fdw.Services.Pipelines): it closes
`PipelineTypeBase` over `IPipeline<TIn, TOut>` and `GraphPipelineConfiguration`, and holds the phase bodies as static methods, `GraphRegistration` and `GraphInitialization`; it sets no phase itself (STC002), so each
sealed closing sets its own, each body one call: `Registration((builder, loggerFactory) => GraphRegistration(this, builder, loggerFactory))`. Registration: the graph
pipeline factory and provider, the graph configuration provider, `PipelineExecutionRegistration.RegisterPipelineExecutionQueue` (queue, run control, request store, wait thresholds,
executor and hosted services, the Queued runner, the run starter), the readers of a data set's writers, the inspection services
(`PipelineExecutionRegistration.RegisterAdditionalServices`), and the domain's provider of
ETL pipelines (`IEtlPipelineProvider`), which each graph pipeline type registers its provider into under its name. Initialization: the graph configuration
provider into the Pipeline domain's provider, and the FDW-6132 check of every builder's needs. `GraphPipelineType` closes it over the ETL run
(`PipelineExecutionOptions` in, `IEtlPipelineExecutionResult` out) as option `Graph` with run mode `Queued`; a closing type states only its name, description
and run mode. Every closing reads its rows from the one graph row container (`GraphPipelineTypeBase.ContainerName`, `pipe.GraphPipelineConfiguration`) and
registers the graph configuration provider under its own name. `IEtlPipeline` is the ETL closing of `IPipeline<TIn, TOut>`. `PipelineExecutionRegistration`
is a static class an orchestration node also calls; both its methods are idempotent.

## The Queued run mode (FDW-6131)

- `QueuedPipelineRunner` is the runner of the `Queued` run mode: it records the run (an execution item, the run row, the parameters given), keeps and queues its
  request for `PipelineExecutionBackgroundService`. A start that names an execution its caller opened is queued as prepared, with no second record; each queue
  refusal of it has its own code (150830 full, 150831 stopped, 90830 not recorded), and `QueuedPipelineRunner.IsQueueRefusal` reads it back as the queue's own result.
- `PipelineRunStarter` (`IPipelineRunStarter`, used by the Execute pipeline component and the orchestration node) is the adapter over `IPipelineRuns`; hosts
  register it with a factory over the run contract.
- A restarted run (`PipelineCutOffRuns` -> `PipelineRunRequestStore.StartAgain`) goes straight to the executor, not through a runner (FDW-3216).
- A run's person (FDW-6137) rides where its roles ride: `PipelineExecutionRequest.PersonId`, the stored request text, `PipelineExecutionOptions.PersonId`,
  `FlowRunIdentity.PersonId` and the start an Execute pipeline component makes. `PipelineExecutionBackgroundService.ResolveRunIdentity` runs a person's
  run as that person (`PipelineRunPerson`). First the account must still pass the sign-in check (`IIssuanceEligibility.MayBeIssued`: not deleted,
  not disabled, not locked out, a member of the run's tenant), because `IUserPermissions.Get` answers a disabled person with their full grants; a
  deny is `RUNPERSON-100833` naming its reason, and a check that cannot be read is `RUNPERSON-70836`. Then the person's permissions are read live
  (unreadable: `RUNPERSON-100831`), and every permission the run's roles carry must be one the person holds (`RUNPERSON-100832`); the tenant is read
  for `globalTenant` (unreadable: `RUNPERSON-70837`). The run's roles themselves were judged for the run's tenant when the run was accepted. The work runs under a `ClaimsPrincipalAuthenticationContext` holding the person, the tenant
  (`globalTenant` for the global one), the run's roles and their permissions, so the connection reads only the person's rows. No token is minted.
  A host that registers no `IIssuanceEligibility` or `ITenantProvider` refuses a person's run (`RUNPERSON-60836`). A run with no person acts as its
  tenant run (`ResolveTenantRun`).
- The graph pipeline layer is `IGraphPipelineConfiguration` (components, links, dropped outputs, variables, parameters, link capacity), implemented by
  `GraphPipelineConfiguration`. `GraphShapeValidator.GraphProblems`, `GraphComponentValidator`, `GraphDescriber` and `GraphFlowBuilder` read a graph through it.

## A component type from another package reaches host services (FDW-6132, FDW-6240)

A `FlowStepBuilders` or `FlowComponentBuilders` option from another package can need a service of its own package (a model client, a tool client) that FDW
has no domain for. Its package writes a build contribution and names it in its builder's `Needs`. It registers nothing: `GraphPipelineType` registers every
type a collected builder needs. FDW's own contracts are not contributions; each is a domain whose provider sits on `FlowBuildContext`, as `ConnectionProvider`
does.

| today | added | neighbor |
|---|---|---|
| A step or component is built by its builder from the component, its built children and the run (`FlowStepBuilderBase.Build`, `FlowBuildContext`) | `IFlowBuildContribution`: a class of the package of the component type that takes its services by constructor, named in the builder's `Needs` and read with `context.Contribution<T>(component)` | `FlowBuildContext.ConnectionProvider` and the other providers on the context, for what FDW owns |

No existing pattern fits, because:
- a step (`IFlowStep`) is not an `IGenericService`, which a service type requires (`ServiceTypeBase.cs:37`);
- `IServiceFactory.Create` takes only the configuration (`IServiceFactory.cs:48-57`), while a step is built from the component, its built children and the run
  (`FlowStepBuilderBase.cs:37`);
- a service type collection holds only service type registrations, not plain options (`ServiceTypeCollectionBase.cs:41`), and `ComponentTypes` must stay free
  of the host.

A worked example, a package that ships a step component type `AskModel`:

```csharp
// The package's own service, with what it takes registered by that service's own domain.
public sealed class ModelContribution(IModelClient client) : IFlowBuildContribution
{
    public IModelClient Client { get; } = client;
}

[TypeOption(typeof(FlowStepBuilders), "AskModel")]
public sealed class AskModelStepBuilder : FlowStepBuilderBase
{
    public AskModelStepBuilder() : base("AskModel") { }

    public override IReadOnlyList<Type> Needs => [typeof(ModelContribution)];

    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        var model = context.Contribution<ModelContribution>(component);
        return model.IsFailure || model.Value is null ? model.ToNewResult<IFlowStep>() : GenericResult<IFlowStep>.Success(new AskModelStep(component.Name, model.Value.Client));
    }
}
// Nothing in Program.cs: referencing the package attaches the builder, and the graph type registers ModelContribution.
```

- `GraphPipelineType`'s Register phase takes the distinct `Needs` of `FlowStepBuilders.All()` and `FlowComponentBuilders.All()` and registers each as a
  scoped `IFlowBuildContribution` (one Trace line per type, `ComponentContributionRegistered`, FLOW-10357). A need that is not a concrete class implementing
  the contract stops the host (`ComponentNeedNotAContribution`, FLOW-60359). A contribution is scoped, as the graph pipeline factory is, so it may take a
  service of the run, such as one read under the run's person.
- Its Initialize constructs them in a scope of their own. A contribution registered twice (`ComponentContributionRegisteredTwice`, FLOW-60361) or one that cannot be constructed, such
  as one whose dependency is not registered (`ComponentContributionUnresolvable`, FLOW-60362), stops the host. One no builder needs is a warning
  (`ComponentContributionUnused`, FLOW-60363). A builder's need the host lacks is `ComponentServicesUnregistered` (FLOW-60356).
- `GraphPipelineFactory` is given the contributions and `FlowBuildContext` requires them. A graph whose component type lacks one is refused before anything
  runs (`ComponentServiceMissing`, FLOW-60355).
- A Graph data set gets them too (FDW-6240). Its runner is held by the data gateway factories, which are singletons, so it takes `IServiceScopeFactory` and each
  read opens a scope, resolves the contributions in it as the host's start does and ends the scope with the read: two reads never share an instance. A contribution that
  cannot be constructed fails the read with `GraphDataSetRunFailed`, naming the data set, and the reason carries `ComponentContributionUnresolvable` (FLOW-60362); a
  component type whose contribution the host did not register fails it with `ComponentServiceMissing` (FLOW-60355).
- A hand registration is refused: in an application, `services.Add*` is FDW056 at compile time; inside a package, a second registration of a contribution the
  graph type registers is FLOW-60361 at Initialize.

### Settings a component type declares (FDW-6133)

A component type from another package can state settings the component itself does not hold: no column (`ComponentSettingBindings`), no child rows
(`ComponentSettingBindings.RowKinds`) and no tool stage. `ComponentSettingBindings.IsDeclared` is that test, and every setting of FDW's own component types fails it. Each
such setting must state its data type as an `IDataType` (`DataType: new DoubleDataType()`). Set `ReadsWhenStepRuns` when its text may name variables as `{{Name}}`.

- The graph states the value as one JSON value. It is kept as invariant text in `pipe.PipelineComponentSetting` (`data.GraphDataSetComponentSetting` for a
  Graph data set), and a host declares both containers in its `configurationSchema.json`.
- A save refuses a required setting left out, a setting the component type does not take, a declared setting with no data type, text that does not convert to the data
  type, and `{{Name}}` text on a setting without `ReadsWhenStepRuns`.
- In the builder, `context.Setting(component, "name")` (`ComponentSettings.Declared`) gives a `ComponentSettingValue`. `Read(variables, logger)` gives the value
  as its data type: the stored text converted, or `{{Name}}` text rendered by `VariableText` when the step runs. Refusals: FLOW-60370 the component type does not take
  the name, 60371 the component holds it itself, 60372 no data type, 30371 not stated, 20370 the text does not convert; at run, 30370 a variable it names has
  no value and 20371 the rendered value does not convert.
- When the host starts, `GraphPipelineType`'s Initialize checks that every component type has exactly one builder of the right collection and every builder
  names a component type (`ComponentTypeHasNoBuilder` FLOW-60357, `ComponentTypeHasTwoBuilders` FLOW-60364, `BuilderNamesNoComponentType` FLOW-60365), and
  names every declared setting with no data type (`ComponentSettingDataTypeNotDeclared`, FLOW-60358), one message each.

## Installation

```bash
dotnet add package Fdw.Services.Etl --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Fluency` · `Fdw.Configuration` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Data.DataSets` · `Fdw.Operations.Abstractions` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Calculations` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Http.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Etl.Abstractions` · `Fdw.Services.EtlGateway` · `Fdw.Services.Notifications.Abstractions` · `Fdw.Services.Pipelines` · `Fdw.Services.Settings` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; Dependencies checked against the csproj on 2026-10-25 -->
