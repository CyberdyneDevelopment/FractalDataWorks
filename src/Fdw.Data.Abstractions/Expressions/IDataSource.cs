namespace Fdw.Data.Abstractions;

/// <summary>
/// Defines a data source for federated queries.
/// Each source specifies a container and connection (datastore).
/// </summary>
public interface IDataSource
{
    /// <summary>
    /// Gets the logical name for this source (used in join conditions).
    /// </summary>
    /// <value>The source name (e.g., "Customers", "Orders").</value>
    string Name { get; }

    /// <summary>
    /// Gets the physical container name.
    /// </summary>
    /// <value>The container name in the datastore (e.g., "dbo.Customers", "/api/orders").</value>
    string ContainerName { get; }

    /// <summary>
    /// Gets the connection name for this source.
    /// </summary>
    /// <value>The connection name (e.g., "DefaultSql", "OrdersApi").</value>
    string ConnectionName { get; }

    /// <summary>
    /// Gets the alias for this source (optional).
    /// </summary>
    /// <value>The alias used in queries (e.g., "c" for Customers).</value>
    string? Alias { get; }

    /// <summary>
    /// Gets the filter to apply to this source before joining.
    /// </summary>
    /// <value>Pre-join filter (predicate pushdown optimization).</value>
    IFilterExpression? Filter { get; }
}
