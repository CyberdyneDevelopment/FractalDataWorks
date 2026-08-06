using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Abstract base class for schema discovery type definitions.
/// Uses CRTP pattern consistent with other TypeOptionBase implementations.
/// </summary>
/// <remarks>
/// Derived types register their schema discoverer implementations during
/// DI registration. Each type handles:
/// <list type="bullet">
/// <item>Phase 1 (<see cref="Register"/>): Register DI services (discoverer, dependencies)</item>
/// </list>
/// Discovery is performed directly via <see cref="ISchemaDiscovery"/>
/// on the ConnectionType — no intermediate provider layer is needed.
/// </remarks>
public abstract class SchemaDiscoveryTypeBase : TypeOptionBase<int, SchemaDiscoveryTypeBase>, ISchemaDiscoveryType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this schema discovery type.</param>
    /// <param name="name">The name of this schema discovery type (e.g., "MsSql").</param>
    /// <param name="displayName">The display name for this schema discovery type.</param>
    /// <param name="description">A description of this schema discovery type.</param>
    /// <param name="category">The category (defaults to "SchemaDiscovery").</param>
    protected SchemaDiscoveryTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        string? category = null)
        : base(id, name, name, displayName, description, category ?? "SchemaDiscovery")
    {
    }

    /// <summary>
    /// Registers the required services for this schema discovery type with the DI container.
    /// Override to register the discoverer implementation and its dependencies.
    /// Called during Phase 1 (before Build).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public abstract IServiceCollection Register(IServiceCollection services);
}
