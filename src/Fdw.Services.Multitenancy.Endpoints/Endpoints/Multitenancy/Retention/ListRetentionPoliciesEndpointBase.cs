using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the retention policies of an organization:
/// <c>GET /organizations/{OrganizationId}/retention/policies</c> (FDW-1575).
/// </summary>
public abstract class ListRetentionPoliciesEndpointBase : Endpoint<OrganizationRetentionRequest, RetentionPoliciesResponse>
{
    private readonly IRetentionPolicies _policies;
    private readonly IOrganizationProvider _organizations;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListRetentionPoliciesEndpointBase"/> class.</summary>
    protected ListRetentionPoliciesEndpointBase(
        ILogger logger, IRetentionPolicies policies, IOrganizationProvider organizations, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _policies = policies;
        _organizations = organizations;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/retention/policies");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRetentionRequest req, CancellationToken ct)
    {
        var access = await RetentionAccess.TenantOf(_organizations, _systemRoles, HttpContext, req.OrganizationId, EndpointLogger, ct).ConfigureAwait(false);
        if (access.Refusal is { } refusal)
        {
            await Send.ResultAsync(refusal).ConfigureAwait(false);
            return;
        }

        var listed = await _policies.List(access.TenantId, req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
