using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// The JSON-serializable shape of a <c>Claim</c>. <see cref="Source"/> carries the
/// <c>ClaimSources</c> option name rather than <c>IClaimSource</c> itself — an interface has no
/// serializable identity, and the name is exactly what <c>ClaimSources.ByName</c> needs to recover it.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ClaimData
{
    /// <summary>Gets or sets the claim type.</summary>
    public required string Type { get; set; }

    /// <summary>Gets or sets the claim value.</summary>
    public required string Value { get; set; }

    /// <summary>Gets or sets the claim's source, by <c>ClaimSources</c> name.</summary>
    public required string Source { get; set; }

    /// <summary>Gets or sets the authority that asserted it, for an external claim.</summary>
    public string? Issuer { get; set; }
}
