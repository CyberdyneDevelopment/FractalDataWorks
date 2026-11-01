using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for making an organization with its starting roles and first administrator: <c>POST /organizations</c>
/// (FDW-1758). The organization is made in the tenant the caller acts in; a platform administrator makes one in another tenant by
/// switching to it first.
/// </summary>
public abstract class CreateOrganizationEndpointBase : Endpoint<CreateOrganizationRequest, OrganizationSummaryResponse>
{
    private readonly IOrganizationFounding _founding;
    private readonly IOrganizationProvider _organizations;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateOrganizationEndpointBase"/> class.</summary>
    protected CreateOrganizationEndpointBase(ILogger logger, IOrganizationFounding founding, IOrganizationProvider organizations, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _founding = founding;
        _organizations = organizations;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CreateOrganizationRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            OrganizationAdminEndpointLog.CallerActsInNoTenant(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var founded = await _founding.Found(new NewOrganization(tenantId, req.Name, req.Slug), req.CopyRolesFrom, req.FirstAdministrator, req.AdministratorRole, ct)
            .ConfigureAwait(false);
        if (founded.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(founded, HttpContext)).ConfigureAwait(false);
            return;
        }

        var organizationId = founded.Value;
        var audited = await OrganizationChangeAudit.Record(_audit, "OrganizationMade", organizationId, null,
            $"Slug='{req.Slug}'; Name='{req.Name}'; FirstAdministrator='{req.FirstAdministrator}'; Role='{req.AdministratorRole}'; RolesFrom='{req.CopyRolesFrom}'", ct)
            .ConfigureAwait(false);
        if (audited.IsFailure)
        {
            OrganizationAdminEndpointLog.ChangeNotAudited(EndpointLogger, organizationId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        OrganizationAdminEndpointLog.OrganizationMade(EndpointLogger, organizationId, tenantId);
        var made = await _organizations.Get(organizationId, ct).ConfigureAwait(false);
        if (made.IsFailure || made.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(made, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(OrganizationMap.Of(made.Value), 201, ct).ConfigureAwait(false);
    }
}
