using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Resiliency.Clients.Abstractions;

/// <summary>
/// HTTP client interface for the FDW Resiliency service API.
/// </summary>
public interface IResiliencyClient
{
    /// <summary>
    /// Lists all configured resiliency policies.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<ResiliencyPolicyDto>>> List(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resiliency policy by its identifier.
    /// </summary>
    Task<IGenericResult<ResiliencyPolicyDto>> Get(
        Guid policyId,
        CancellationToken cancellationToken = default);
}
