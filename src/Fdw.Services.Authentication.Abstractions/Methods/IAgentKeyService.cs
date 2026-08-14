using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>
/// Manages the lifecycle of agent keys — API keys that grant AI agents (Claude, OpenAI, etc.)
/// access to WebMCP endpoints, bound to a specific user identity for RBAC.
/// The raw key value is returned only at creation time and is never stored in plain text.
/// </summary>
public interface IAgentKeyService
{
    /// <summary>
    /// Creates a new agent key for the specified user.
    /// The raw key value is returned only in this response.
    /// </summary>
    /// <param name="userId">The ID of the user the agent acts on behalf of.</param>
    /// <param name="userName">The display name of the owning user.</param>
    /// <param name="label">A human-readable label to identify the key.</param>
    /// <param name="expiresAt">Optional UTC expiration date. Pass <c>null</c> for a non-expiring key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<AgentKeyCreatedResult>> CreateKey(
        Guid userId,
        string userName,
        string label,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a presented raw agent key and resolves the user the agent acts on behalf of.
    /// </summary>
    /// <remarks>
    /// Without this, a key can be minted and listed but can never authenticate — the agent_key
    /// grant has no edge to call.
    /// </remarks>
    /// <param name="rawKey">The raw key presented by the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<AgentKeyValidationResult>> ValidateKey(
        string rawKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active agent keys for the specified user (summary view only — no raw values).
    /// </summary>
    /// <param name="userId">The ID of the user whose keys to list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<AgentKeySummary>>> ListKeys(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes (deactivates) a single agent key by ID. The key must belong to the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the key.</param>
    /// <param name="keyId">The ID of the key to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult> DeleteKey(
        Guid userId,
        Guid keyId,
        CancellationToken cancellationToken = default);
}
