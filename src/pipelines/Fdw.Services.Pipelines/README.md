# Fdw.Services.Pipelines

The Pipeline domain — `PipelineTypes`, the pipeline types FDW can run, its configuration provider and run contract — and the pipeline services beside it.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `PipelineServiceTypes` | class | ServiceTypeCollection for the pipeline-service domain (gateway-backed pipeline configuration provider).… |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultPipelineServiceType` | class | Default pipeline-service type. Registers the gateway-backed that the pipeline endpoints depend on. |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `PipelineConfiguration` | class | General header configuration for pipeline services representing the pipe.Pipeline parent table. The… |

## The run contract (FDW-6131)

`PipelineRuns` implements `IPipelineRuns`: it reads the pipeline, takes its pipeline type from the implementation its domain row names (`PipelineTypes.ByName`),
takes the type's run mode (`PipelineRunModes.ByName(type.RunModeName)`) and hands the run to the runner registered for that mode (runners come by constructor). It
refuses, each with its own code (type code `PIPELINERUNS`): an implementation no pipeline type names (`PipelineTypeUnknown`, 60830), a mode no runner here carries out
(`PipelineRunModeUnsupported`, 60831), a pipeline type that names an undeclared mode (`PipelineTypeRunModeUnknown`, 60832), naming the pipeline by both or neither
(20830), a wait on a run not started here to be awaited (20831), an unreadable pipeline (30830) or run (30831). `Stop` delegates to `IExecutionRunControl`.
`PipelineTypes` registers it.

## The Pipeline domain (FDW-6244)

`PipelineTypes` is the Pipeline domain's service type collection (category `Pipeline`): its options are the domain's implementations, found by the
`Implementation` a `pipe.Pipeline` domain row names. Each option derives from `PipelineTypeBase<TService, TFactory, TConfiguration>` and states its run mode;
every option implements the non-generic `IPipelineType`, so an implementation over its own closed base (`GraphPipelineTypeBase<TIn, TOut>` in Fdw.Services.Etl,
closed by `GraphPipelineType`, option `Graph`, run mode `Queued`) is found like any other. The domain registers the `pipe.Pipeline` configuration provider and
`IPipelineRuns`; every option requires the provider (`ConfigurationProviderNotRegistered`, `PIPELINES-60833`). Before the options initialize, the domain
proves each option's run mode, one message per gap: a run mode no `PipelineRunModes` option declares stops every host (`RunModeNotDeclared`,
`PIPELINES-60834`), and in a host that runs pipelines (one that names `PipelineTypes.OperationalConnection`) a run mode no registered `IPipelineRunner`
carries out stops it (`RunModeHasNoRunner`, `PIPELINES-60835`). After every option has initialized, a pipeline type with no row provider registered under
its name on the pipe.Pipeline domain provider stops the host (`ImplementationProviderNotRegistered`, `PIPELINES-60837`): a pipe.Pipeline row names an
implementation, so each must be readable, and a type that sets no phase registers none. Every gap is reported in one pass. A type states its run mode by name: the option itself would be a constructor parameter the
generated NotFound sentinel cannot build (TC009). `PipelineTypes.OperationalConnection` is the
store of the runs, which the host names (no default). `PipelineServiceTypes` stays a separate collection (category `PipelineService`) for stored scripts,
webhook triggers and the owner of a new pipeline.

## The run's person (FDW-6137)

A run acts as the person who asked for it. `PipelineRuns.Start` fills `PipelineStart.PersonId` from the authenticated caller
(`IAuthenticationContextAccessor.Current`) and from nothing else, by `RunPerson.Of`: a caller is a person when it is authenticated, is not the
system, is not a `TenantRunAuthenticationContext` and names a Guid user, the rule the connection uses to read a caller's own rows. A caller that is
no person (the system, a run acting as no person, a machine client) starts a run that acts as no person. A start that names any other person is
refused (`RunPersonMismatch`, 100830): a start a run makes names its run's person, so it is taken only when that run is the caller.

A start that releases a held run (`PipelineStartOpened.ReleasesHeldRun`) acts as its `PersonId`, the person who asked for it, whom the host stamped
(with `RunPerson.Of`) on the request it kept when it held the run, and never as the caller releasing it: the releaser's authority
covers the release alone. A held run asked for by no person acts as no person. The person is kept in the stored request text, not in a column of its
own; a column comes with the first need to find runs by person.

## Installation

```bash
dotnet add package Fdw.Services.Pipelines --prerelease
```

## Dependencies

`Fdw.Commands.Data.Fluency` · `Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Messages` · `Fdw.Operations.Abstractions` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Pipelines.Abstractions` · `Fdw.Services.Workspaces.Abstractions` · `Fdw.SignalR` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; Dependencies checked against the csproj on 2026-10-25 -->
