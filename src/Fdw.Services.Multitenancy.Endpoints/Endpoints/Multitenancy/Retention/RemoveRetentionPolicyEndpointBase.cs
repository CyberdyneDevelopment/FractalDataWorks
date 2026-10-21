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
/// Generic base endpoint for removing the retention policy of a document type:
/// <c>DELETE /organizations/{OrganizationId}/retention/policies/{DocumentType}</c> (FDW-1575). The removal is recorded in the administrative
/// audit at the organization level.
/// </summary>
public abstract class RemoveRetentionPolicyEndpointBase : Endpoint<RemoveRetentionPolicyRequest>
{
    private readonly IRetentionPolicies _policies;
    private readonly IOrganizationProvider _organizations;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="RemoveRetentionPolicyEndpointBase"/> class.</summary>
    protected RemoveRetentionPolicyEndpointBase(
        ILogger logger, IRetentionPolicies policies, IOrganizationProvider organizations, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _policies = policies;
        _organizations = organizations;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/organizations/{OrganizationId}/retention/policies/{DocumentType}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(RemoveRetentionPolicyRequest req, CancellationToken ct)
    {
        var access = await RetentionAccess.TenantOf(_organizations, _systemRoles, HttpContext, req.OrganizationId, EndpointLogger, ct).ConfigureAwait(false);
        if (access.Refusal is { } refusal)
        {
            await Send.ResultAsync(refusal).ConfigureAwait(false);
            return;
        }

        var removed = await _policies.Remove(access.TenantId, req.OrganizationId, req.DocumentType, ct).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(removed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var itemId = $"{req.OrganizationId}:{req.DocumentType}";
        var audited = await RetentionAudit.Record(_audit, "RetentionPolicyRemoved", "RetentionPolicy", itemId, req.OrganizationId, req.DocumentType, null, null, ct)
            .ConfigureAwait(false);
        if (audited.IsFailure)
        {
            RetentionEndpointLog.ChangeNotAudited(EndpointLogger, "RetentionPolicyRemoved", itemId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
