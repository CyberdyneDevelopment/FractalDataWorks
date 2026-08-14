namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// A managed-identity mechanism available in this deployment.
/// </summary>
public sealed class IdentityMechanismDto
{
    /// <summary>Gets or sets the mechanism name — the value an identity configuration's ServiceOptionType carries.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable description of the mechanism.</summary>
    public string Description { get; set; } = string.Empty;
}
