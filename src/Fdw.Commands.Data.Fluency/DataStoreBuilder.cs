namespace Fdw.Commands.Data;

/// <summary>
/// Intermediate builder for selecting a path within a data store.
/// </summary>
public class DataStoreBuilder
{
    private readonly string _dataStoreName;

    internal DataStoreBuilder(string dataStoreName)
    {
        _dataStoreName = dataStoreName;
    }

    /// <summary>
    /// Select a path (schema) within this DataStore (required).
    /// </summary>
    /// <param name="pathName">The path name (e.g., "auth", "cfg", "dbo").</param>
    /// <returns>A builder for selecting containers within this path.</returns>
    /// <example>
    /// <code>
    /// var command = DataStores.For("AuthDb")
    ///     .Path("auth")
    ///     .Container&lt;User&gt;("Users")
    ///     .Where("Username", username)
    ///     .Build();
    /// </code>
    /// </example>
    public DataStorePathBuilder Path(string pathName)
    {
        return new DataStorePathBuilder(_dataStoreName, pathName);
    }
}