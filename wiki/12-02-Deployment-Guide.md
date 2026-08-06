# Deployment Guide

The deployment story is owned by each reference repo and the `infrastructure` repo, not by the framework. This page lists the moving parts and pointers.

## Components

| Component | Source | Deployment artifact |
|-----------|--------|---------------------|
| FDW framework | this repo | NuGet packages on the GitLab feed |
| Reference API server | `reference-api` repo | Published .NET app (e.g. on VM 104) |
| Reference ETL server | `reference-etl` repo | Published .NET app |
| Reference Scheduler server | `reference-scheduler` repo | Published .NET app |
| Reference UI (Server) | `reference-ui` repo | Published .NET app + Tailwind static assets |
| Reference UI (Auto) | `reference-aui` repo | Published .NET app + MudBlazor assets |
| Databases | `databases` repo | dacpacs (ConfigurationDb, OpsDb, AuthDb, DataDb) |
| Infrastructure config | `infrastructure` repo | Proxmox / nginx / Caddy / preview-slot config |

## Build and Pack

The framework is packed to NuGet via `public/scripts/pack-local.sh` (local feed) or pushed to the GitLab feed via `public/scripts/push-gitlab.sh`. See [17-01 Build Pipeline Guide](17-01-Build-Pipeline-Guide.md) for the framework build pipeline.

Reference apps are built and published from their own repos. Each repo has its own pipeline; refer to that repo's `README.md` for canonical instructions.

## Database Deployment

Database dacpacs deploy via `sqlpackage /Action:Publish`. See [17-02 Database Setup](17-02-Docker-And-Database-Setup.md) for the dacpac layout and the per-database `security/permissions.sql` scripts.

The expected deployment sequence is documented in [12-13 OpsDb Configuration](12-13-OpsDb-Configuration.md) and [17-02 Database Setup](17-02-Docker-And-Database-Setup.md):

1. Publish ConfigurationDb dacpac
2. Publish OpsDb dacpac
3. Publish AuthDb dacpac (and DataDb on the data-hosting server)
4. Run ConfigurationDb seed scripts (registers OpsDb/AuthDb/DataDb as connections)
5. Start FDW services

## Secret Provisioning

All service login passwords resolve from `FDW_SECRET_*` environment variables via the `EnvSecrets` secret manager (seeded by default). For production deployments on systemd, set the variables in the unit file's `[Service]` `Environment=` directives:

```
Environment=FDW_SECRET_CONFIG_PASSWORD=...
Environment=FDW_SECRET_AUTH_PASSWORD=...
Environment=FDW_SECRET_OPS_PASSWORD=...
```

For Azure deployments, replace `EnvSecrets` with `AzureKeyVault` per [12-10 Secret Management](12-10-Secret-Management.md).

## Reverse Proxy / HTTPS

For preview environments, this team uses Cloudflare → Proxmox nginx → Caddy → app. App servers must clear `ForwardedHeaders.KnownNetworks` and `KnownProxies` so the `X-Forwarded-Proto: https` from Caddy is trusted:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { },
});
```

This must run before `app.UseAuthentication()` so cookie security flags and redirect URIs see the correct scheme.

## See Also

- [12-04 Security Hardening](12-04-Security-Hardening.md) — OWASP headers, CORS, DB isolation
- [12-10 Secret Management](12-10-Secret-Management.md) — secret-manager wiring
- [17-01 Build Pipeline Guide](17-01-Build-Pipeline-Guide.md) — framework build/pack
- [17-02 Database Setup](17-02-Docker-And-Database-Setup.md) — dacpac deployment
