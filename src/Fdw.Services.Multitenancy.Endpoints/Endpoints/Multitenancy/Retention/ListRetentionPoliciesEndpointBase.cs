using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
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

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListRetentionPoliciesEndpointBase"/> class.</summary>
    protected ListRetentionPoliciesEndpointBase(
        ILogger logger, IRetentionPolicies policies)
    {
        EndpointLogger = logger;
        _policies = policies;
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
        var listed = await _policies.List(req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
