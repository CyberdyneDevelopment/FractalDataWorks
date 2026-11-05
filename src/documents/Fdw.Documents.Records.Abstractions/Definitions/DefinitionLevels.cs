namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>
/// The names of the levels a definition is held at (FDW-3398): the same names FDW-1579's settings use for the same levels
/// (<c>SettingScope.PlatformLevel</c>, <c>TenantLevel</c> and <c>OrganizationLevel</c>).
/// </summary>
public static class DefinitionLevels
{
    /// <summary>The platform, above every tenant.</summary>
    public const string Platform = "Platform";

    /// <summary>A tenant.</summary>
    public const string Tenant = "Tenant";

    /// <summary>An organization of a tenant.</summary>
    public const string Organization = "Organization";
}
