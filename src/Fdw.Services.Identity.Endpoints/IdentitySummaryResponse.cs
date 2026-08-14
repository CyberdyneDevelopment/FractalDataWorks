using System;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Summary view of a configured managed identity.
/// </summary>
/// <remarks>
/// Carries no credential and no token. An identity's whole value is that the secret stays where it
/// was put, so the read surface exposes which identity exists and how it authenticates, never what
/// it authenticates with.
/// </remarks>
public sealed class IdentitySummaryResponse
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this identity is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the mechanism this identity uses (the ServiceOptionType).</summary>
    public string? Mechanism { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
}
