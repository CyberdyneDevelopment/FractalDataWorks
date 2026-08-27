using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// One fact about a principal, and where it came from.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record Claim
{
    /// <summary>Gets the claim type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the claim value.</summary>
    public required string Value { get; init; }

    /// <summary>Gets where this claim came from.</summary>
    public required ClaimSource Source { get; init; }

    /// <summary>Gets the authority that asserted it, for an external claim.</summary>
    public string? Issuer { get; init; }
}
