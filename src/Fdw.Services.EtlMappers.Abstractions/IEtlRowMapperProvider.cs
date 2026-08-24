using Fdw.Results;

namespace Fdw.Services.EtlMappers.Abstractions;

/// <summary>
/// Provider interface for ETL row mappers.
/// Acts as a factory registry for creating mapper instances.
/// </summary>
/// <remarks>
/// Unlike IPlatformServiceProvider, this provider creates mappers on-demand from configuration
/// rather than looking them up by name/id. Mappers are lightweight utilities, not full services.
/// </remarks>
public interface IEtlRowMapperProvider
{
    /// <summary>
    /// Creates a mapper using the specified configuration.
    /// </summary>
    /// <param name="configuration">The mapper configuration.</param>
    /// <returns>A result containing the created mapper or failure information.</returns>
    IGenericResult<IEtlRowMapper> Create(EtlRowMapperConfiguration configuration);

    /// <summary>
    /// Registers a mapper factory for a service option type.
    /// </summary>
    /// <param name="serviceOptionType">The service type name (e.g., "Pooled", "Dynamic").</param>
    /// <param name="factory">The factory to register.</param>
    void Register(string serviceOptionType, IEtlRowMapperFactory factory);

    /// <summary>
    /// Gets the default mapper type name.
    /// </summary>
    string DefaultMapperType { get; }
}
