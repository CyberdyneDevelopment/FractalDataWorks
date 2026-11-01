using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Clients.Models;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Fdw.Services.Users;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for making a tenant together with its first administrator (FDW-1578).
/// </summary>
/// <remarks>
/// Only a platform administrator makes a tenant. The permission policy on the host's endpoint says who may call at all; the platform
/// administrator check here is the second half, because the policy is held by roles below the platform too. The endpoint resolves the
/// person, hands the work to <see cref="ITenantFounding"/>, records the change in the audit and answers with the tenant as it now is.
/// </remarks>
public abstract class CreateTenantEndpointBase : Endpoint<CreateTenantRequest, TenantDetailPayload>
{
    private readonly ITenantFounding _founding;
    private readonly ITenantProvider _tenants;
    private readonly IUserConfigurationProvider _users;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateTenantEndpointBase"/> class.</summary>
    protected CreateTenantEndpointBase(
        ILogger logger,
        ITenantFounding founding,
        ITenantProvider tenants,
        IUserConfigurationProvider users,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _founding = founding;
        _tenants = tenants;
        _users = users;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tenants");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CreateTenantRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            TenantEndpointLog.PlatformAdministratorRequired(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        if (await FindAdministrator(req.FirstAdministrator, ct).ConfigureAwait(false) is not { } administratorId)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var founded = await _founding
            .Found(new NewTenant(req.Name, req.Slug, req.OrgPrefix, req.ConnectionName), administratorId, req.AdministratorRole, ct)
            .ConfigureAwait(false);
        if (founded.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(founded, HttpContext)).ConfigureAwait(false);
            return;
        }

        TenantEndpointLog.TenantMade(EndpointLogger, founded.Value, req.Slug, administratorId);
        await Answer(founded.Value, req, administratorId, ct).ConfigureAwait(false);
    }

    // The route and the body name the person by id or by user name; either way they must already have an account.
    private async Task<Guid?> FindAdministrator(string idOrName, CancellationToken ct)
    {
        var found = Guid.TryParse(idOrName, out var id)
            ? await _users.Get(id, ct).ConfigureAwait(false)
            : await _users.Get(idOrName, ct).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } user ? user.Id : null;
    }

    // Records the change, then answers with the tenant as the rest of the system reads it.
    private async Task Answer(Guid tenantId, CreateTenantRequest req, Guid administratorId, CancellationToken ct)
    {
        var audited = await TenantChangeAudit.Record(_audit, "TenantMade", tenantId, null,
            $"Slug='{req.Slug}'; Name='{req.Name}'; FirstAdministrator='{administratorId}'; Role='{req.AdministratorRole}'", ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            TenantEndpointLog.ChangeNotAudited(EndpointLogger, tenantId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var tenant = await _tenants.GetTenant(tenantId, ct).ConfigureAwait(false);
        if (!tenant.IsSuccess || tenant.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(tenant, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(new TenantDetailPayload
        {
            Id = tenant.Value.Id,
            Name = tenant.Value.DisplayName,
            Slug = tenant.Value.Slug,
            IsActive = tenant.Value.IsActive,
            ConnectionName = tenant.Value.ConnectionName,
            AvailableRoles = [.. tenant.Value.AvailableRoles],
        }, 201, ct).ConfigureAwait(false);
    }
}
