namespace Fdw.Commands.Data;

/// <summary>
/// Direct factory method for creating query builders with full path specification.
/// </summary>
public static class DataQuery
{
    /// <summary>
    /// Get a query builder with full path specification (DataStore, Path, Container).
    /// All three parameters are required.
    /// </summary>
    /// <typeparam name="T">The result type for queries on this container.</typeparam>
    /// <param name="dataStoreName">The DataStore name (e.g., "AuthDb", "ConfigurationDb").</param>
    /// <param name="pathName">The path within the DataStore (e.g., "auth", "cfg", "dbo").</param>
    /// <param name="containerName">The container name (table/endpoint).</param>
    /// <returns>A new QueryCommandBuilder with full path specification.</returns>
    /// <example>
    /// <code>
    /// // Query Users from AuthDb.auth schema
    /// var command = DataQuery.From&lt;User&gt;("AuthDb", "auth", "Users")
    ///     .Where("Username", username)
    ///     .Build();
    /// </code>
    /// </example>
    public static QueryCommandBuilder<T> From<T>(string dataStoreName, string pathName, string containerName)
    {
        return new QueryCommandBuilder<T>(dataStoreName, pathName, containerName);
    }
}