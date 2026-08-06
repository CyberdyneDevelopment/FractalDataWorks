namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a navigation path to containers within a data store.
/// </summary>
public interface IPath
{
    /// <summary>
    /// String representation of the path.
    /// </summary>
    string PathValue { get; }

    /// <summary>
    /// Domain this path belongs to (Sql, Rest, File, GraphQL).
    /// </summary>
    string Domain { get; }
}
