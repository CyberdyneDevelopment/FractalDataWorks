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
/// Generic base endpoint for setting the retention policy of a document type:
/// <c>PUT /organizations/{OrganizationId}/retention/policies/{DocumentType}</c> (FDW-1575). The policy is recorded in the administrative
/// audit at the organization level.
/// </summary>
public abstract class SetRetentionPolicyEndpointBase : Endpoint<SetRetentionPolicyRequest, RetentionPolicyResponse>
{
    private readonly IRetentionPolicies _policies;
    private readonly IOrganizationProvider _organizations;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SetRetentionPolicyEndpointBase"/> class.</summary>
    protected SetRetentionPolicyEndpointBase(
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
        Put("/organizations/{OrganizationId}/retention/policies/{DocumentType}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetRetentionPolicyRequest req, CancellationToken ct)
    {
        var access = await RetentionAccess.TenantOf(_organizations, _systemRoles, HttpContext, req.OrganizationId, EndpointLogger, ct).ConfigureAwait(false);
        if (access.Refusal is { } refusal)
        {
            await Send.ResultAsync(refusal).ConfigureAwait(false);
            return;
        }

        var set = await _policies.Set(
            access.TenantId, req.OrganizationId, new NewRetentionPolicy(req.DocumentType, req.Years, req.Months, req.Days, req.StartField, req.EndAction), ct)
            .ConfigureAwait(false);
        if (set.IsFailure || set.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(set, HttpContext)).ConfigureAwait(false);
            return;
        }

        var itemId = $"{req.OrganizationId}:{set.Value.DocumentType}";
        var audited = await RetentionAudit.Record(_audit, "RetentionPolicySet", "RetentionPolicy", itemId, req.OrganizationId, null,
            $"{set.Value.Years}y {set.Value.Months}m {set.Value.Days}d from '{set.Value.StartField}', then '{set.Value.EndAction}'", null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            RetentionEndpointLog.ChangeNotAudited(EndpointLogger, "RetentionPolicySet", itemId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(set.Value), ct).ConfigureAwait(false);
    }
}
