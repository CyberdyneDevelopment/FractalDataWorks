using System;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Detailed view of a configured managed identity.
/// </summary>
/// <remarks>
/// Carries no credential and no token, for the same reason as <see cref="IdentitySummaryResponse"/>.
/// Where the mechanism resolves a secret, this reports WHERE it is resolved from — the secret manager
/// and key name — which is what an operator needs to fix a misconfiguration, and is not itself secret.
/// </remarks>
public sealed class IdentityDetailResponse
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this identity is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the mechanism this identity uses (the ServiceOptionType).</summary>
    public string? Mechanism { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the issuer this identity obtains tokens from.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets the tenant this identity belongs to; null means system-wide.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets when this configuration was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets when this configuration was last modified.</summary>
    public DateTimeOffset? ModifyDate { get; set; }
}
