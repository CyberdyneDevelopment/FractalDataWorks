using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Provider for resiliency policy configurations.
/// Reads server defaults and tenant-scoped policies from ConfigurationDb via DataGateway.
/// </summary>
public interface IResiliencyPolicyProvider
{
    /// <summary>
    /// Gets the resiliency policy configuration by its identifier.
    /// </summary>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The policy configuration, or failure if not found.</returns>
    Task<IGenericResult<IGenericConfiguration>> Get(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the server default resiliency policy identifier, if configured.
    /// </summary>
    /// <returns>The server default policy identifier, or <c>null</c> if not set.</returns>
    Guid? GetServerDefaultPolicyId();
}
