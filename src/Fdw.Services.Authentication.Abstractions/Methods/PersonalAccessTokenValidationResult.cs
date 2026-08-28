using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>Result of validating a Personal Access Token.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PersonalAccessTokenValidationResult
{
    /// <summary>Gets or sets whether the token is valid and active.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the user ID associated with the token, when valid.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the token ID, when valid.</summary>
    public Guid TokenId { get; set; }
}
