namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Dynamic tenant created from configuration at runtime.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ConfiguredTenant : TenantTypeBase
{
    /// <summary>
    /// Creates a tenant from configuration.
    /// </summary>
    public ConfiguredTenant(TenantConfiguration configuration)
        : base(configuration)
    {
    }
}
