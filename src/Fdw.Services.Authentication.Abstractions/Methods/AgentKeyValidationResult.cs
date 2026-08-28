using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>Result of validating an agent key.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AgentKeyValidationResult
{
    /// <summary>Gets or sets whether the key is valid, active and unexpired.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the user the agent acts on behalf of, when valid.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the key ID, when valid.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Gets or sets the surrogate key identifier, when valid.</summary>
    /// <remarks>
    /// Carried alongside <see cref="KeyId"/> because <c>agent.AgentAction.AgentKeyId</c> is the
    /// identity column, not the Guid. A consumer that had only the Guid would have to read the key
    /// row back to record an action against it.
    /// </remarks>
    public int AgentKeyId { get; set; }

    /// <summary>Gets or sets the human-readable label of the key, when valid.</summary>
    /// <remarks>
    /// Part of the validation result rather than looked up separately because every consumer that
    /// cares an agent is acting — an audit row, a log line, a message attributed on screen — needs
    /// to say WHICH agent, and a second read to answer that is a second chance to answer it
    /// differently.
    /// </remarks>
    public string Label { get; set; } = string.Empty;
}
