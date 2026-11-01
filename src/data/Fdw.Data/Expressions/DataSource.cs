using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IDataSource for federated queries.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class DataSource : IDataSource
{
    /// <summary>
    /// Gets or inits the logical name for this source.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or inits the physical container name.
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    /// Gets or inits the connection name.
    /// </summary>
    public required string ConnectionName { get; init; }

    /// <summary>
    /// Gets or inits the alias for this source.
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Gets or inits the filter for this source.
    /// </summary>
    public IFilterExpression? Filter { get; init; }
}
