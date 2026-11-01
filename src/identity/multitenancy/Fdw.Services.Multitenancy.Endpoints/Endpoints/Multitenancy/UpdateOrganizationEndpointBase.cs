using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for renaming, disabling and enabling an organization: <c>PATCH /organizations/{OrganizationId}</c> (FDW-1758).
/// The caller must act in the tenant that owns the organization, or be a platform administrator. The default organization of a tenant cannot
/// be disabled.
/// </summary>
public abstract class UpdateOrganizationEndpointBase : Endpoint<UpdateOrganizationRequest, OrganizationSummaryResponse>
{
    private readonly IOrganizationWriter _writer;
    private readonly IOrganizationProvider _organizations;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="UpdateOrganizationEndpointBase"/> class.</summary>
    protected UpdateOrganizationEndpointBase(
        ILogger logger, IOrganizationWriter writer, IOrganizationProvider organizations, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _writer = writer;
        _organizations = organizations;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/organizations/{OrganizationId}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateOrganizationRequest req, CancellationToken ct)
    {
        var before = await _organizations.Get(req.OrganizationId, ct).ConfigureAwait(false);
        if (before.IsFailure || before.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(before, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!FeatureCaller.MayActFor(User, before.Value.TenantId, _systemRoles))
        {
            OrganizationAdminEndpointLog.OtherTenant(EndpointLogger, before.Value.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var written = await _writer.Update(req.OrganizationId, new OrganizationChange(req.Name, req.IsActive), ct).ConfigureAwait(false);
        if (written.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(written, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Answer(before.Value, req, ct).ConfigureAwait(false);
    }

    // The parts of the request that change the organization, as (kind, old, new); a part equal to what the organization has is not a change.
    private static List<(string Kind, string? Old, string New)> Changes(OrganizationConfiguration before, UpdateOrganizationRequest req)
    {
        var changes = new List<(string, string?, string)>();
        if (req.Name is { } name && !string.Equals(name, before.Name, StringComparison.Ordinal))
            changes.Add(("OrganizationRenamed", before.Name, name));
        if (req.IsActive is { } active && active != before.IsActive)
            changes.Add((active ? "OrganizationEnabled" : "OrganizationDisabled", before.IsActive.ToString(), active.ToString()));
        return changes;
    }

    // Records each change, then answers with the organization as the rest of the system reads it now.
    private async Task Answer(OrganizationConfiguration before, UpdateOrganizationRequest req, CancellationToken ct)
    {
        var changes = Changes(before, req);
        foreach (var (kind, oldValue, newValue) in changes)
        {
            var audited = await OrganizationChangeAudit.Record(_audit, kind, req.OrganizationId, oldValue, newValue, ct).ConfigureAwait(false);
            if (audited.IsFailure)
            {
                OrganizationAdminEndpointLog.ChangeNotAudited(EndpointLogger, req.OrganizationId, audited.CurrentMessage);
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }
        }

        OrganizationAdminEndpointLog.OrganizationChanged(EndpointLogger, req.OrganizationId, string.Join(", ", changes.ConvertAll(change => change.Kind)));
        var after = await _organizations.Get(req.OrganizationId, ct).ConfigureAwait(false);
        if (after.IsFailure || after.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(after, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(OrganizationMap.Of(after.Value), ct).ConfigureAwait(false);
    }
}
