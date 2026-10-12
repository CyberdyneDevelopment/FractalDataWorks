namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Asks whether a configured identity can actually obtain a token right now.
/// </summary>
public sealed class VerifyIdentityRequest
{
    /// <summary>Gets or sets the identity configuration name to verify.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the audience to request a token for.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the space-delimited scopes to request. Optional.</summary>
    public string? Scopes { get; set; }
}
