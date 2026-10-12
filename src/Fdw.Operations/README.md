# Fdw.Operations

The operations domain: execution tracking, escalation policies and the workflow state machine.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `OperationsTypes` | class | ServiceTypeCollection for operations domain service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultOperationsServiceType` | class | Default operations service type. Registers execution tracking (IExecutionTracker), escalation… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `EscalationLevelConfiguration` | class | Configuration for individual escalation levels within an escalation policy. Defines delay, recipients,… |
| `EscalationLevelRecipientConfiguration` | class | Configuration for an individual escalation recipient. Child of EscalationLevelConfiguration. |
| `EscalationPolicyConfiguration` | class | Configuration for escalation policies. Defines when and how to notify stakeholders about execution… |

## Installation

```bash
dotnet add package Fdw.Operations --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Operations.Abstractions` · `Fdw.Services` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Notifications` · `Fdw.Services.Notifications.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
