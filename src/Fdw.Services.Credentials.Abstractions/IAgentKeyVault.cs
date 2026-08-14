using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Narrow per-domain interface for the agent-key vault. The raw key is minted by the service edge; the
/// vault peppers and stores it, and only ever returns metadata — never a hash or raw key.
/// <see cref="List"/> returns label/dates only.
/// </summary>
/// <remarks>
/// The freshly-minted raw key is passed in solely to be peppered inside the vault (the pepper is
/// vault-only); it is never stored in the clear, logged, or returned.
/// </remarks>
public interface IAgentKeyVault : IDataVault
{
    /// <summary>
    /// Peppers and stores the minted raw key and returns the new key id. The raw key is never stored
    /// in the clear or returned by the vault.
    /// </summary>
    /// <param name="userId">The user the agent acts on behalf of.</param>
    /// <param name="userName">The display name of the owning user.</param>
    /// <param name="rawKey">The freshly-minted raw key, peppered and stored by the vault.</param>
    /// <param name="label">A human-readable label to identify the key.</param>
    /// <param name="expiresAt">Optional UTC expiration; <c>null</c> for non-expiring.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<Guid>> Create(Guid userId, string userName, string rawKey, string label, DateTime? expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a presented raw key against the stored peppered hash in constant time, honouring
    /// active state and expiry, and resolves the user the agent acts on behalf of.
    /// </summary>
    /// <remarks>
    /// The key identifies its own owner, so no user id is supplied. A vault that also implements
    /// <see cref="IPatVault"/> carries an identically-shaped token validator, so at least one of the
    /// two is an explicit interface implementation on the concrete vault.
    /// </remarks>
    /// <param name="rawKey">The raw key presented by the caller.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<AgentKeyValidationResult>> Validate(string rawKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns metadata summaries for a user's active keys — never a hash or raw key.
    /// </summary>
    /// <param name="userId">The user whose keys to list.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<AgentKeySummary>>> List(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes (deactivates) a single key by id, verifying ownership.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="keyId">The key to delete.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Delete(Guid userId, Guid keyId, CancellationToken cancellationToken = default);
}
