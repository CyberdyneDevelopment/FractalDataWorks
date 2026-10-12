namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Provider for row mapper factories, enabling lookup by type name.
/// </summary>
public interface IRowMapperProvider
{
    /// <summary>
    /// Gets a row mapper factory by type name.
    /// </summary>
    /// <param name="typeName">The mapper type name (e.g., "Pooled", "Dynamic").</param>
    /// <returns>The mapper factory, or null if not found.</returns>
    IRowMapperFactory? GetFactory(string typeName);

    /// <summary>
    /// Gets the default row mapper factory.
    /// </summary>
    /// <returns>The default factory.</returns>
    IRowMapperFactory GetDefaultFactory();

    /// <summary>
    /// Registers a factory with the provider.
    /// </summary>
    /// <param name="typeName">The mapper type name.</param>
    /// <param name="factory">The factory to register.</param>
    void Register(string typeName, IRowMapperFactory factory);
}
