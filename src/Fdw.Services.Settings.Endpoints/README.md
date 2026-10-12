# Fdw.Services.Settings.Endpoints

Endpoint bases for layered settings.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `CreateRoleSettingEndpointBase` | class | Base endpoint for creating a role-level setting override. |
| `CreateServerSettingEndpointBase` | class | Base endpoint for creating a new server-level setting. |
| `CreateTenantSettingEndpointBase` | class | Base endpoint for creating a tenant-level setting override. |
| `GetServerSettingEndpointBase` | class | Base endpoint for getting a server-level setting by name. |
| `ListRoleSettingsEndpointBase` | class | Base endpoint for listing role-level setting overrides. |
| `ListServerSettingsEndpointBase` | class | Base endpoint for listing all server-level settings. |
| `ListTenantSettingsEndpointBase` | class | Base endpoint for listing tenant-level setting overrides. |
| `UpdateRoleSettingEndpointBase` | class | Base endpoint for updating a role-level setting override. |
| `UpdateServerSettingEndpointBase` | class | Base endpoint for updating an existing server-level setting. |
| `UpdateTenantSettingEndpointBase` | class | Base endpoint for updating a tenant-level setting override. |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `CreateRoleSettingRequest` | class | Request DTO for creating a role-level setting override. |
| `CreateServerSettingRequest` | class | Request DTO for creating a new server-level setting. |
| `CreateTenantSettingRequest` | class | Request DTO for creating a tenant-level setting override. |
| `SettingNameRequest` | class | Request DTO that identifies a setting by name. |
| `UpdateRoleSettingRequest` | class | Request DTO for updating a role-level setting override. |
| `UpdateServerSettingRequest` | class | Request DTO for updating a server-level setting. |
| `UpdateTenantSettingRequest` | class | Request DTO for updating a tenant-level setting override. |

## Installation

```bash
dotnet add package Fdw.Services.Settings.Endpoints --prerelease
```

## Dependencies

`Fdw.Hosting` · `Fdw.MessageLogging.Abstractions` · `Fdw.Services.Abstractions` · `Fdw.Services.Settings` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
