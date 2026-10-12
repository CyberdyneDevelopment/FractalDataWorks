# Fdw.Services.Notifications

The notification domain — a collection of delivery channels resolved by name.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `NotificationResultCodes` | class | TypeCollection for Notification service result codes. EventId range: 6200-6249 (within Services… |
| `NotificationTypes` | class | ServiceTypeCollection for all notification service implementations. The source generator populates this… |

## Options (0 declared)

| Type | Kind | Purpose |
|---|---|---|
| `NotificationTypes` | class | ServiceTypeCollection for all notification service implementations. The source generator populates this… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `NotificationConditionConfiguration` | class | Configuration for notification conditions. Generates the table notify.NotificationCondition as a child… |
| `NotificationConfiguration` | class | Parent (header) configuration class for all notification types. Generates the parent table… |
| `NotificationRecipientConfiguration` | class | Configuration for notification recipients. Generates the table notify.NotificationRecipient as a child… |
| `NotificationRuleConfiguration` | class | Configuration for notification rules. Generates the table notify.NotificationRule. |

## Installation

```bash
dotnet add package Fdw.Services.Notifications --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Notifications.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
