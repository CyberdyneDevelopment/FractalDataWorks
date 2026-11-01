using System;

namespace Fdw.Services.Settings;

/// <summary>
/// The level of the tenant and organization tree a setting is read or written at (FDW-1579).
/// </summary>
/// <remarks>A platform scope names no tenant, a tenant scope names its tenant, and an organization scope names its organization (its tenant is found from it).</remarks>
public sealed class SettingScope
{
    /// <summary>The name of the platform level, above every tenant.</summary>
    public const string PlatformLevel = "Platform";

    /// <summary>The name of the tenant level.</summary>
    public const string TenantLevel = "Tenant";

    /// <summary>The name of the organization level, below its tenant.</summary>
    public const string OrganizationLevel = "Organization";

    private SettingScope(string level, Guid? tenantId, Guid? organizationId)
    {
        Level = level;
        TenantId = tenantId;
        OrganizationId = organizationId;
    }

    /// <summary>Gets the level's name: <see cref="PlatformLevel"/>, <see cref="TenantLevel"/> or <see cref="OrganizationLevel"/>.</summary>
    public string Level { get; }

    /// <summary>Gets the tenant, for a tenant scope.</summary>
    public Guid? TenantId { get; }

    /// <summary>Gets the organization, for an organization scope.</summary>
    public Guid? OrganizationId { get; }

    /// <summary>Makes the scope of the platform.</summary>
    /// <returns>The platform scope.</returns>
    public static SettingScope ForPlatform() => new(PlatformLevel, null, null);

    /// <summary>Makes the scope of a tenant.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <returns>The tenant scope.</returns>
    public static SettingScope ForTenant(Guid tenantId) => new(TenantLevel, tenantId, null);

    /// <summary>Makes the scope of an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The organization scope.</returns>
    public static SettingScope ForOrganization(Guid organizationId) => new(OrganizationLevel, null, organizationId);
}
