using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for acting in another department: <c>POST /organizations/switch</c> (FDW-1802). The caller keeps the company
/// they act in and asks for a new token that also carries the department. Whether they may act there is the issuance check every token
/// passes: the department is in their company, is active, and they hold access in it. Like the switch of company, a person who may not is
/// told so (403) before the flow runs. The switch is recorded in the administrative audit at the Organization level, naming the department.
/// </summary>
public abstract class SwitchOrganizationEndpointBase : Endpoint<SwitchOrganizationRequest, SwitchOrganizationDto>
{
    private readonly IOrganizationProvider _organizations;
    private readonly ITenantProvider _tenants;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SwitchOrganizationEndpointBase"/> class.</summary>
    protected SwitchOrganizationEndpointBase(ILogger logger, IOrganizationProvider organizations, ITenantProvider tenants, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _organizations = organizations;
        _tenants = tenants;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/switch");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <summary>
    /// Issues the caller a new access token for the company they act in and the department they asked for. Switching is re-issuing, never a header
    /// or a session flag: the department on the token is the one every endpoint reads.
    /// </summary>
    /// <param name="userId">The caller.</param>
    /// <param name="tenantId">The company the caller acts in.</param>
    /// <param name="organization">The department to act in.</param>
    /// <param name="cancellationToken">Cancels the issue.</param>
    /// <returns>The token and when it expires, or the reason the caller may not act in the department.</returns>
    protected abstract Task<IGenericResult<(string AccessToken, DateTimeOffset ExpiresAt)>> GenerateNewToken(
        Guid userId, Guid tenantId, OrganizationConfiguration organization, CancellationToken cancellationToken);

    /// <inheritdoc />
    public override async Task HandleAsync(SwitchOrganizationRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimDefinitions.sub.Name)?.Value, out var userId)
            || !Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            OrganizationSwitchEndpointLog.CallerNotIdentified(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var organization = await _organizations.Get(req.OrganizationId, ct).ConfigureAwait(false);
        if (organization.IsFailure || organization.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(organization, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (organization.Value.TenantId != tenantId)
        {
            OrganizationSwitchEndpointLog.OtherTenant(EndpointLogger, userId, organization.Value.Id, tenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var access = await _tenants.ValidateOrganizationAccess(tenantId, organization.Value.Id, userId.ToString(), ct).ConfigureAwait(false);
        if (access.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(access, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!access.Value)
        {
            OrganizationSwitchEndpointLog.AccessDenied(EndpointLogger, userId, organization.Value.Id);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var token = await GenerateNewToken(userId, tenantId, organization.Value, ct).ConfigureAwait(false);
        if (token.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(token, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await OrganizationChangeAudit.Record(_audit, "OrganizationSwitched", organization.Value.Id, null, null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            OrganizationSwitchEndpointLog.SwitchNotAudited(EndpointLogger, userId, organization.Value.Id, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        OrganizationSwitchEndpointLog.Switched(EndpointLogger, userId, organization.Value.Id, tenantId);
        await Send.OkAsync(
            new SwitchOrganizationDto
            {
                Success = true,
                AccessToken = token.Value.AccessToken,
                ExpiresAt = token.Value.ExpiresAt,
                Organization = OrganizationMap.Of(organization.Value),
            },
            ct).ConfigureAwait(false);
    }
}
