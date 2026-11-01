using Fdw.Results;

namespace Fdw.Services.EtlMappers.Abstractions;

/// <summary>
/// Factory interface for creating ETL row mapper instances.
/// </summary>
/// <typeparam name="TMapper">The mapper type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the mapper.</typeparam>
public interface IEtlRowMapperFactory<TMapper, TConfiguration>
    where TMapper : IEtlRowMapper
    where TConfiguration : EtlRowMapperConfiguration
{
    /// <summary>
    /// Creates a mapper instance with the specified configuration.
    /// </summary>
    /// <param name="configuration">The mapper configuration.</param>
    /// <returns>A result containing the created mapper or failure information.</returns>
    IGenericResult<TMapper> Create(TConfiguration configuration);
}

/// <summary>
/// Non-generic factory interface for creating ETL row mapper instances.
/// </summary>
public interface IEtlRowMapperFactory
{
    /// <summary>
    /// Creates a new mapper instance with the specified configuration.
    /// </summary>
    /// <param name="configuration">The mapper configuration.</param>
    /// <returns>A result containing the created mapper or failure information.</returns>
    IGenericResult<IEtlRowMapper> Create(EtlRowMapperConfiguration configuration);
}
