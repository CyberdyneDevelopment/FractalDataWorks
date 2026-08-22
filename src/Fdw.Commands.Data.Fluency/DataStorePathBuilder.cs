namespace Fdw.Commands.Data;

/// <summary>
/// Intermediate builder for selecting a container within a DataStore path.
/// </summary>
public class DataStorePathBuilder
{
    private readonly string _dataStoreName;
    private readonly string _pathName;

    internal DataStorePathBuilder(string dataStoreName, string pathName)
    {
        _dataStoreName = dataStoreName;
        _pathName = pathName;
    }

    /// <summary>
    /// Select a container (table/endpoint) to query within this path.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="containerName">The container name (table/endpoint).</param>
    /// <returns>A new QueryCommandBuilder for fluent query construction.</returns>
    public QueryCommandBuilder<T> Container<T>(string containerName)
    {
        return new QueryCommandBuilder<T>(_dataStoreName, _pathName, containerName);
    }
}