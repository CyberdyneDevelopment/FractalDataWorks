namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Factory interface for creating row mappers.
/// </summary>
public interface IRowMapperFactory
{
    /// <summary>
    /// Creates a new row mapper instance.
    /// </summary>
    /// <returns>A new row mapper.</returns>
    IRowMapper Create();
}

/// <summary>
/// Factory interface for creating row mappers with configuration.
/// </summary>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
public interface IRowMapperFactory<in TConfiguration> : IRowMapperFactory
    where TConfiguration : class
{
    /// <summary>
    /// Creates a new row mapper instance with the specified configuration.
    /// </summary>
    /// <param name="configuration">The mapper configuration.</param>
    /// <returns>A new row mapper.</returns>
    IRowMapper Create(TConfiguration configuration);
}
