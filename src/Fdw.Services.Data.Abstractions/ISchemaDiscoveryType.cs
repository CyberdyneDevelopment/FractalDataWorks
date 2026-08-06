using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Interface for schema discovery type options.
/// Each implementation represents a store-type-specific schema discoverer
/// (e.g., MsSql, PostgreSql) that can register its DI services.
/// </summary>
public interface ISchemaDiscoveryType : ITypeOption<int, SchemaDiscoveryTypeBase>
{
    /// <summary>
    /// Registers the required services for this schema discovery type with the DI container.
    /// Called during Phase 1 (before Build).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    IServiceCollection Register(IServiceCollection services);
}
