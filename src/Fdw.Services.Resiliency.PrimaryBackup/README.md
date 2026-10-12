# Fdw.Services.Resiliency.PrimaryBackup

The primary/backup strategy — on primary failure, read from the backup data set and schedule a refresh.

This package declares 1 interface(s), 1 configuration type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `PrimaryBackupResiliencyType` | class | PrimaryBackup resiliency strategy. On primary source failure: 1. Activates the configured backup data… |

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IPrimaryBackupResiliencyContext` | interface | Extended execution context for the PrimaryBackup strategy. Provides access to the for triggering the… |

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `PrimaryBackupResiliencyConfiguration` | class | Configuration for the PrimaryBackup resiliency strategy. Fields map to the… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `PrimaryBackupLog` | class | MessageLogging methods for PrimaryBackup resiliency strategy. |

## Installation

```bash
dotnet add package Fdw.Services.Resiliency.PrimaryBackup --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Services.Resiliency` · `Fdw.Services.Scheduling.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
