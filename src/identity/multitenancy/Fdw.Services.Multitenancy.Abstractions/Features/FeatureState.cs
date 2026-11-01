namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Whether a feature is on for a tenant or an organization, and when it is off, which level turned it off (FDW-1580).
/// </summary>
/// <param name="Name">The feature's name.</param>
/// <param name="DisplayName">What the Features tab calls the feature.</param>
/// <param name="Description">What the feature gives the people who have it.</param>
/// <param name="Enabled">True when the feature is on at this level and every level above it.</param>
/// <param name="OffAt">When the feature is off, the level that has not switched it on: <see cref="PlatformLevel"/> or <see cref="TenantLevel"/>; null when it is on.</param>
public sealed record FeatureState(string Name, string DisplayName, string Description, bool Enabled, string? OffAt)
{
    /// <summary>The platform, which switches a feature on for a tenant.</summary>
    public const string PlatformLevel = "Platform";

    /// <summary>The tenant, which switches a feature on for its organizations.</summary>
    public const string TenantLevel = "Tenant";

    /// <summary>The organization, which a feature is switched on for; the audit records a switch for one at this level.</summary>
    public const string OrganizationLevel = "Organization";
}
