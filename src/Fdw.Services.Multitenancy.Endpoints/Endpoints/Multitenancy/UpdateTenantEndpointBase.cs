using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Clients.Models;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for renaming, disabling, enabling or re-connecting a tenant (FDW-1578).
/// </summary>
/// <remarks>
/// Only a platform administrator changes a tenant. A part the request leaves out keeps its value. Each part that changes is recorded in
/// the audit with its old and new value; a request that changes nothing records nothing.
/// </remarks>
public abstract class UpdateTenantEndpointBase : Endpoint<UpdateTenantRequest, TenantDetailPayload>
{
    private readonly ITenantWriter _writer;
    private readonly ITenantProvider _tenants;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="UpdateTenantEndpointBase"/> class.</summary>
    protected UpdateTenantEndpointBase(
        ILogger logger,
        ITenantWriter writer,
        ITenantProvider tenants,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _writer = writer;
        _tenants = tenants;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/tenants/{Id}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateTenantRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            TenantEndpointLog.PlatformAdministratorRequired(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var before = await _tenants.GetTenant(req.Id, ct).ConfigureAwait(false);
        if (!before.IsSuccess || before.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var written = await _writer.Update(req.Id, new TenantChange(req.Name, req.IsActive, req.ConnectionName), ct).ConfigureAwait(false);
        if (written.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(written, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Answer(before.Value, req, ct).ConfigureAwait(false);
    }

    // The parts of the request that change the tenant, as (kind, old, new); a part equal to what the tenant has is not a change.
    private static List<(string Kind, string? Old, string New)> Changes(ITenant before, UpdateTenantRequest req)
    {
        var changes = new List<(string, string?, string)>();
        if (req.Name is { } name && !string.Equals(name, before.DisplayName, StringComparison.Ordinal))
            changes.Add(("TenantRenamed", before.DisplayName, name));
        if (req.IsActive is { } active && active != before.IsActive)
            changes.Add((active ? "TenantEnabled" : "TenantDisabled", before.IsActive.ToString(), active.ToString()));
        if (req.ConnectionName is { } connection && !string.Equals(connection, before.ConnectionName, StringComparison.Ordinal))
            changes.Add(("TenantConnectionChanged", before.ConnectionName, connection));
        return changes;
    }

    // Records each change, then answers with the tenant as the rest of the system reads it now.
    private async Task Answer(ITenant before, UpdateTenantRequest req, CancellationToken ct)
    {
        var changes = Changes(before, req);
        foreach (var (kind, oldValue, newValue) in changes)
        {
            var audited = await TenantChangeAudit.Record(_audit, kind, req.Id, oldValue, newValue, ct).ConfigureAwait(false);
            if (audited.IsFailure)
            {
                TenantEndpointLog.ChangeNotAudited(EndpointLogger, req.Id, audited.CurrentMessage);
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }
        }

        TenantEndpointLog.TenantChanged(EndpointLogger, req.Id, string.Join(", ", changes.ConvertAll(change => change.Kind)));
        var after = await _tenants.GetTenant(req.Id, ct).ConfigureAwait(false);
        if (!after.IsSuccess || after.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(after, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new TenantDetailPayload
        {
            Id = after.Value.Id,
            Name = after.Value.DisplayName,
            Slug = after.Value.Slug,
            IsActive = after.Value.IsActive,
            ConnectionName = after.Value.ConnectionName,
            AvailableRoles = [.. after.Value.AvailableRoles],
        }, ct).ConfigureAwait(false);
    }
}
