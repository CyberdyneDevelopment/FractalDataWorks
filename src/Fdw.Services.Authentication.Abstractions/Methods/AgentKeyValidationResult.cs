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
}
