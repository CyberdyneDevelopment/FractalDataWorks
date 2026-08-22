namespace Fdw.Commands.Data;

/// <summary>
/// Alias for DataQuery for shorter syntax.
/// </summary>
public static class Query
{
    /// <summary>
    /// Get a query builder with full path specification (DataStore, Path, Container).
    /// All three parameters are required.
    /// </summary>
    /// <typeparam name="T">The result type for queries on this container.</typeparam>
    /// <param name="dataStoreName">The DataStore name.</param>
    /// <param name="pathName">The path within the DataStore.</param>
    /// <param name="containerName">The container name.</param>
    /// <returns>A new QueryCommandBuilder with full path specification.</returns>
    public static QueryCommandBuilder<T> From<T>(string dataStoreName, string pathName, string containerName)
    {
        return DataQuery.From<T>(dataStoreName, pathName, containerName);
    }
}