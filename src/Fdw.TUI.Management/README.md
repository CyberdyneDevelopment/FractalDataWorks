# Fdw.TUI.Management

The terminal management console.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `ManagementServiceRegistrations` | class | TypeCollection of management service registrations. Use ManagementServiceRegistrations.RegisterAll() to… |
| `MenuTargets` | class | TypeCollection of available menu targets. Use MenuTargets.ByName() for dispatch-based menu navigation. |
| `NavigationActions` | class | TypeCollection for navigation actions. |

## Options (12 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationMenuTarget` | class | Menu target for configuration management. |
| `ConnectMenuTarget` | class | Menu target for connecting to an instance. |
| `ConnectionManagerRegistration` | class | Service registration for the connection manager. |
| `ExitMenuTarget` | class | Menu target for exiting the application. |
| `ManagementServiceRegistrationBase` | class | Abstract base class for management service registrations. Inherit from this class and apply [TypeOption]… |
| `MenuTargetBase` | class | Abstract base class for menu targets. Inherit from this class and apply [TypeOption] attribute to create… |
| `MonitoringMenuTarget` | class | Menu target for monitoring and logs. |
| `SettingsMenuTarget` | class | Menu target for application settings. |
| `SettingsServiceRegistration` | class | Service registration for the settings service. |
| `ExitNavigationAction` | class | — |
| `PopNavigationAction` | class | — |
| `PushNavigationAction` | class | — |
| `ReplaceNavigationAction` | class | — |
| `StayNavigationAction` | class | — |

## Installation

```bash
dotnet add package Fdw.TUI.Management --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Services.Authorization.Clients` · `Fdw.Services.Connections.Clients` · `Fdw.Services.Data.Clients` · `Fdw.Services.Etl.Projects.Clients` · `Fdw.Services.Multitenancy.Clients` · `Fdw.Services.Notifications.Clients` · `Fdw.Services.Pipelines.Clients` · `Fdw.Services.Quality.Clients` · `Fdw.Services.Registration` · `Fdw.Services.Scheduling.Clients` · `Fdw.Services.SecretManagers.Clients` · `Fdw.Services.Settings.Clients` · `Fdw.Services.Users.Clients` · `Fdw.UI.Components` · `Fdw.UI.Rendering.Spectre` · `Fdw.UI.Themes` · `Fdw.Web.Clients` · `Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators` · `Fdw.Registration.SourceGenerators` · `Fdw.Services.Registration.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
