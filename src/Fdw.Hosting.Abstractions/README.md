# Fdw.Hosting.Abstractions

The hosting contracts and options a host binds at startup.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (9)

| Type | Kind | Purpose |
|---|---|---|
| `IConfigurationConnectionNameProvider` | interface | Provides the connection name used by endpoint base classes to query configuration data. Replaces… |
| `IFdwHost` | interface | Represents a built FDW host that can be started and stopped. |
| `IFdwHostApplicationLifetime` | interface | Provides notifications for application lifetime events. |
| `IFdwHostBuilder` | interface | Builder for creating FDW hosts. |
| `IFdwHostBuilderContext` | interface | Context available during host building. Provides access to configuration and properties for modules. |
| `IFdwHostLifetime` | interface | Abstracts the host lifetime for managing startup and shutdown. |
| `ILogLevel` | interface | Represents a logging level for Serilog configuration. |
| `ISink` | interface | Represents a logging sink type for Serilog configuration. |
| `ITelemetryExporter` | interface | Represents a telemetry exporter type for OpenTelemetry configuration. |

## Base types (3)

| Type | Kind | Purpose |
|---|---|---|
| `LogLevelBase` | class | Base class for log level TypeOptions. |
| `SinkBase` | class | Base class for sink TypeOptions. |
| `TelemetryExporterBase` | class | Base class for telemetry exporter TypeOptions. |

## Models and supporting types (38)

| Type | Kind | Purpose |
|---|---|---|
| `AuthenticationOptions` | class | Authentication options for database connections. |
| `ConfigurationConnectionOptions` | class | Configuration database connection options. Binds to the "FdwHost:Configuration" section in… |
| `ConsoleExporter` | class | Console exporter - writes telemetry data to console for debugging. |
| `ConsoleSink` | class | Console sink - writes logs to standard output. |
| `ConsoleSinkOptions` | class | Console sink configuration options. |
| `DebugLogLevel` | class | Debug log level - detailed information useful during development. |
| `DefaultConfigurationConnectionNameProvider` | class | Default implementation of that reads the connection name from via IOptionsMonitor. |
| `EnvironmentNames` | class | Well-known environment names. |
| `ErrorLogLevel` | class | Error log level - failures that prevent specific operations. |
| `FatalLogLevel` | class | Fatal log level - critical failures requiring immediate attention. |
| `FdwHostOptions` | class | Root options for FDW host configuration. Binds to the "FdwHost" section in appsettings.json. |
| `FeatureOptions` | class | Feature flags for enabling/disabling optional FDW modules. Binds to the "FdwHost:Features" section in… |

## Installation

```bash
dotnet add package Fdw.Hosting.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
