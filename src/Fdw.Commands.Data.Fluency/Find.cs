namespace Fdw.Commands.Data;

/// <summary>
/// Fluent entry point for building find (cross-field search) commands.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for creating find commands:
/// <code>
/// var command = Find.In&lt;Customer&gt;("CRM", "sales", "Customers")
///     .Search("acme")
///     .InFields("Name", "Description")
///     .CaseSensitive(false)
///     .MaxResults(50)
///     .Build();
/// </code>
/// </para>
/// </remarks>
public static class Find
{
    /// <summary>
    /// Starts building a find command with full path specification (DataStore, Path, Container).
    /// All three parameters are required.
    /// </summary>
    /// <typeparam name="T">The result type for records in this container.</typeparam>
    /// <param name="dataStoreName">The DataStore name (e.g., "AuthDb", "PlatformConfiguration").</param>
    /// <param name="pathName">The path within the DataStore (e.g., "auth", "cfg", "dbo").</param>
    /// <param name="containerName">The container name (table/endpoint).</param>
    /// <returns>A new <see cref="FindCommandBuilder{T}"/> with full path specification.</returns>
    public static FindCommandBuilder<T> In<T>(string dataStoreName, string pathName, string containerName)
    {
        return new FindCommandBuilder<T>(dataStoreName, pathName, containerName);
    }
}
