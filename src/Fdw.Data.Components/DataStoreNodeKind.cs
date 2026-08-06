namespace Fdw.Data.Components;

/// <summary>
/// Canonical kind values for <see cref="DataStoreNode.Kind"/>.
/// Consumers must compare with <c>StringComparison.Ordinal</c>.
/// </summary>
public static class DataStoreNodeKind
{
    /// <summary>Root DataStore node.</summary>
    public const string DataStore = "DataStore";

    /// <summary>Intermediate path node beneath a DataStore.</summary>
    public const string Path = "Path";

    /// <summary>Leaf container node beneath a path.</summary>
    public const string Container = "Container";
}
