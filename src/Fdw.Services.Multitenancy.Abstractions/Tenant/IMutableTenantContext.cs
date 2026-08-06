namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Mutable tenant context for setting the current tenant.
/// </summary>
public interface IMutableTenantContext : ITenantContext
{
    /// <summary>
    /// Sets the current tenant.
    /// </summary>
    void SetTenant(ITenant tenant);

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    void Clear();
}
