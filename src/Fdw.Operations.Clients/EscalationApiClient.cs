namespace Fdw.Operations.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for escalation policy management endpoints.
/// </summary>
public class EscalationApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationApiClient"/> class.
    /// </summary>
    public EscalationApiClient(HttpClient httpClient, ILogger<EscalationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all escalation policies.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of escalation policies.</returns>
    public Task<IGenericResult<IReadOnlyList<EscalationPolicyPayload>>> GetPolicies(CancellationToken ct = default)
        => GetList<EscalationPolicyPayload>("escalation/policies", ct);

    /// <summary>
    /// Gets a specific escalation policy by identifier.
    /// </summary>
    /// <param name="id">The policy identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the escalation policy.</returns>
    public Task<IGenericResult<EscalationPolicyPayload>> GetPolicy(Guid id, CancellationToken ct = default)
        => Get<EscalationPolicyPayload>($"escalation/policies/{id}", ct);

    /// <summary>
    /// Creates a new escalation policy.
    /// </summary>
    /// <param name="request">The policy data to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created escalation policy.</returns>
    public Task<IGenericResult<EscalationPolicyPayload>> CreatePolicy(CreateEscalationPolicyRequest request, CancellationToken ct = default)
        => Post<CreateEscalationPolicyRequest, EscalationPolicyPayload>("escalation/policies", request, ct);

    /// <summary>
    /// Updates an existing escalation policy.
    /// </summary>
    /// <param name="id">The policy identifier.</param>
    /// <param name="request">The updated policy data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated escalation policy.</returns>
    public Task<IGenericResult<EscalationPolicyPayload>> UpdatePolicy(Guid id, UpdateEscalationPolicyPayload request, CancellationToken ct = default)
        => Patch<UpdateEscalationPolicyPayload, EscalationPolicyPayload>($"escalation/policies/{id}", request, ct);

    /// <summary>
    /// Deletes an escalation policy.
    /// </summary>
    /// <param name="id">The policy identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public Task<IGenericResult> DeletePolicy(Guid id, CancellationToken ct = default)
        => Delete($"escalation/policies/{id}", ct);
}
