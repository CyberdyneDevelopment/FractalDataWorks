using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The retention policies of an organization: one per document type (FDW-1575).
/// </summary>
/// <remarks>
/// Who may set a policy is settled by the caller, which passes the tenant and the organization together. A policy names a document type
/// and the field its period counts from; it never names the application's document store.
/// </remarks>
public interface IRetentionPolicies
{
    /// <summary>Reads every policy of an organization.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The organization's policies, one per document type.</returns>
    Task<IGenericResult<IReadOnlyList<RetentionPolicy>>> List(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Sets the policy of a document type, replacing the one it had.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="policy">The policy to set.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The policy as it now stands, or a bad-request failure when the period is empty or negative or a name is blank.</returns>
    Task<IGenericResult<RetentionPolicy>> Set(Guid tenantId, Guid organizationId, NewRetentionPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>Removes the policy of a document type.</summary>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentType">The document type whose policy is removed.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or a not-found failure when the organization has no policy for that type.</returns>
    Task<IGenericResult> Remove(Guid tenantId, Guid organizationId, string documentType, CancellationToken cancellationToken = default);
}
