using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>The wire shape of a <c>Claim</c>. <see cref="Source"/> is the claim source's Name.</summary>
[ExcludeFromCodeCoverage]
internal sealed class ClaimDto
{
    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string? Issuer { get; set; }
}
